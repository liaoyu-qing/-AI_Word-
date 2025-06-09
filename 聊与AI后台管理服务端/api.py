from sql import SqlPlugin

import uvicorn
from fastapi import APIRouter, FastAPI, Request, UploadFile, File
from fastapi.encoders import jsonable_encoder
from fastapi.exceptions import HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse, Response
from sse_starlette.sse import EventSourceResponse
from fastapi.staticfiles import StaticFiles
import traceback
import socketio
from openai import OpenAI

import os
import json

class ApiConfig():
    host: str
    port: int

def api_middleware(app: FastAPI):
    rich_available = False
    try:
        if os.environ.get("WEBUI_RICH_EXCEPTIONS", None) is not None:
            import anyio  # importing just so it can be placed on silent list
            import starlette  # importing just so it can be placed on silent list
            from rich.console import Console

            console = Console()
            rich_available = True
    except Exception:
        pass

    def handle_exception(request: Request, e: Exception):
        err = {
            "error": type(e).__name__,
            "detail": vars(e).get("detail", ""),
            "body": vars(e).get("body", ""),
            "errors": str(e),
        }
        if not isinstance(
            e, HTTPException
        ):  # do not print backtrace on known httpexceptions
            message = f"API error: {request.method}: {request.url} {err}"
            if rich_available:
                print(message)
                console.print_exception(
                    show_locals=True,
                    max_frames=2,
                    extra_lines=1,
                    suppress=[anyio, starlette],
                    word_wrap=False,
                    width=min([console.width, 200]),
                )
            else:
                traceback.print_exc()
        return JSONResponse(
            status_code=vars(e).get("status_code", 500), content=jsonable_encoder(err)
        )

    @app.middleware("http")
    async def exception_handling(request: Request, call_next):
        try:
            return await call_next(request)
        except Exception as e:
            return handle_exception(request, e)

    @app.exception_handler(Exception)
    async def fastapi_exception_handler(request: Request, e: Exception):
        return handle_exception(request, e)

    @app.exception_handler(HTTPException)
    async def http_exception_handler(request: Request, e: HTTPException):
        return handle_exception(request, e)

    cors_options = {
        "allow_methods": ["*"],
        "allow_headers": ["*"],
        "allow_origins": ["*"],
        "allow_credentials": True,
        "expose_headers": ["X-Seed"]
    }
    app.add_middleware(CORSMiddleware, **cors_options)

class Api:
    def __init__(self, app: FastAPI, config: ApiConfig):
        self.app = app
        self.config = config
        self.router = APIRouter()
        api_middleware(self.app)

        work_dir = os.path.dirname(os.path.abspath(__file__))
        database = os.path.join(work_dir, "database")
        self.nlp_sql_plugin = SqlPlugin(database, "NlpApiKeys")

        self.config_path = os.path.join(work_dir, 'config.json')

        self.add_api_route("/api/v1/forward_msgs/v1/chat/completions/text/stream", self.api_forward_openai_msg_text_stream, methods=["POST"])
        self.add_api_route("/api/v1/query_api_key_info", self.api_query_api_key_info, methods=["GET"])

        global global_sio
        self.sio = socketio.AsyncServer(async_mode="asgi", cors_allowed_origins="*")
        self.combined_asgi_app = socketio.ASGIApp(self.sio, self.app)
        self.app.mount("/ws", self.combined_asgi_app)
        global_sio = self.sio

    def add_api_route(self, path: str, endpoint, **kwargs):
        return self.app.add_api_route(path, endpoint, **kwargs)
    
    def launch(self):
        self.app.include_router(self.router)
        uvicorn.run(
            self.combined_asgi_app,
            host=self.config.host,
            port=self.config.port,
            timeout_keep_alive=60,
        )

    def query_api_key_info(self, api_key:str):
        ret = self.nlp_sql_plugin.query_by_union_id(api_key, "UsedTokens,UsedPromptTokens,Tokens,JULIANDAY(ExpiryDate)-JULIANDAY(DATE('now'))")
        if ret == None:
            raise HTTPException(400, detail='ERR: API KEY[{}]查询不到'.format(api_key))
        used_tokens, used_prompt_tokens, tokens, datediff = ret
        return used_tokens, used_prompt_tokens, tokens, datediff

    def api_query_api_key_info(self, api_key:str):
        used_tokens, used_prompt_tokens, tokens, datediff = self.query_api_key_info(api_key)
        if datediff < 0:
            datediff = 0
        output = {
            "used_tokens": used_tokens + used_prompt_tokens,
            "tokens": tokens,
            "datediff": datediff
        }
        return Response(content=json.dumps(output), status_code=200)

    async def api_forward_openai_msg_text_stream(self, request: Request):
        body = await request.json()
        union_id = body['api_key']

        used_tokens, used_prompt_tokens, tokens, diffdate = self.query_api_key_info(union_id)
        if diffdate < 0:
            return Response(content='ERR: 此API KEY包含的tokens超过有效期')
        if tokens <= 0:
            return Response(content='ERR: 此API KEY包含的tokens不足')
        used_tokens = self.nlp_sql_plugin.query_by_union_id(union_id, 'UsedTokens')
        if used_tokens == None:
            print("union id is invaild.  ", union_id)
            return Response("API KEY error", status_code=401)
        used_prompt_tokens = self.nlp_sql_plugin.query_by_union_id(union_id, 'UsedPromptTokens')

        used_tokens = used_tokens[0]
        used_prompt_tokens = used_prompt_tokens[0]

        msgs = json.loads(body['messages'])
        model_name = body['model']

        def record_tokens(iter):
            nonlocal used_prompt_tokens
            nonlocal used_tokens
            nonlocal tokens

            prompt_tokens = 0
            completion_tokens = 0
            for js_str in iter:
                js = json.loads(js_str)
                if js["usage"] != None:
                    prompt_tokens = js["usage"]["prompt_tokens"]
                    completion_tokens = js["usage"]["completion_tokens"]
                
                if len(js["choices"]) == 0:
                    continue
                yield json.dumps(js)

            if prompt_tokens < 0:
                prompt_tokens = 0
            if completion_tokens < 0:
                completion_tokens = 0

            used_prompt_tokens += prompt_tokens
            used_tokens += completion_tokens
            tokens -= prompt_tokens
            tokens -= completion_tokens

            self.nlp_sql_plugin.update_by_union_id(union_id, 'UsedTokens', used_tokens)
            self.nlp_sql_plugin.update_by_union_id(union_id, 'UsedPromptTokens', used_prompt_tokens)
            self.nlp_sql_plugin.update_by_union_id(union_id, 'Tokens', tokens)
        
        iter = self.openai_api(msgs, self.config_path, model_name, True)
        return EventSourceResponse(record_tokens(iter))

    def openai_api(self, msgs, conf_path, model_name, stream=True):
        with open(conf_path, 'r') as file:
            str = file.read()
            conf_js = json.loads(str)
            api_key = conf_js["model_map"][model_name]["api_info"]["api_key"]
            api_url = conf_js["model_map"][model_name]["api_info"]["url"]
            model = conf_js["model_map"][model_name]["api_info"]["model"]
            if api_key == "":
                detail = 'ERR: 第三方api key是空的, api url[{}], model[{}]'.format(api_url, model)
                print(detail)
                raise HTTPException(500, detail=detail)

        client = OpenAI(
            api_key=api_key, 
            base_url=api_url,
        )
        response = client.chat.completions.create(
            model = model,
            messages = msgs,
            stream = stream,
            stream_options = {"include_usage": True}
            )

        if stream:
            def stream_response(res):
                for chunk in res:
                    if chunk:
                        yield chunk.model_dump_json()

            return stream_response(response)
        else:
            print("TEST00000 ", response.model_dump_json())
            return response.model_dump_json()
        
    def api_query_api_key_info(self, api_key:str):
        used_tokens, used_prompt_tokens, tokens, datediff = self.query_api_key_info(api_key)
        if datediff < 0:
            datediff = 0
        output = {
            "used_tokens": used_tokens + used_prompt_tokens,
            "tokens": tokens,
            "datediff": datediff
        }
        return Response(content=json.dumps(output), status_code=200)