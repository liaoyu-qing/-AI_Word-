from sql import SqlPlugin
from api import api_middleware, ApiConfig

import uvicorn
from fastapi import APIRouter, FastAPI, Response
from fastapi.staticfiles import StaticFiles
from fastapi.exceptions import HTTPException
import socketio

import os
import json
import uuid

class ManagerApi:
    def __init__(self, app: FastAPI, config: ApiConfig):
        self.app = app
        self.config = config
        self.router = APIRouter()
        api_middleware(self.app)

        work_dir = os.path.dirname(os.path.abspath(__file__))
        database = os.path.join(work_dir, "database")
        self.nlp_sql_plugin = SqlPlugin(database, "NlpApiKeys")
        self.nlp_sql_plugin.create_table("""
            'id' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            'UnionId' VARCHAR(100) NOT NULL,
            'UsedTokens' BIGINT DEFAULT '0',
            'UsedPromptTokens' BIGINT DEFAULT '0',
            'Tokens' BIGINT DEFAULT '0',
            'ExpiryDate' DATE DEFAULT '2000-01-01',
            'ManagerUid' VARCHAR(50)
            """)

        self.add_api_route("/api/v1/manager/query_keys", self.api_query_keys, methods=["GET"])
        self.add_api_route("/api/v1/manager/add_keys", self.api_add_keys, methods=["GET"])

        self.app.mount("/", StaticFiles(directory=os.path.join(work_dir, "assets"), html=True), name="assets")

        global global_sio
        self.sio = socketio.AsyncServer(async_mode="asgi", cors_allowed_origins="*")
        self.combined_asgi_app = socketio.ASGIApp(self.sio, self.app)
        self.app.mount("/ws", self.combined_asgi_app)
        global_sio = self.sio

    def launch(self):
        self.app.include_router(self.router)
        uvicorn.run(
            self.combined_asgi_app,
            host=self.config.host,
            port=self.config.port,
            timeout_keep_alive=60,
        )

    def add_api_route(self, path: str, endpoint, **kwargs):
        return self.app.add_api_route(path, endpoint, **kwargs)
    
    def api_query_keys(self, manager_uid:str):
        print("api_query_keys")
        ret = self.nlp_sql_plugin.query_by_key("ManagerUid", manager_uid, "UnionId,UsedTokens,UsedPromptTokens,Tokens,JULIANDAY(ExpiryDate)-JULIANDAY(DATE('now'))")
        if ret == None:
            raise HTTPException(400, detail='ERR: 该用户未查询到KEY')
        
        output = []
        for tup in ret:
            dict = {
                "key": tup[0],
                "used_tokens": tup[1] + tup[2],
                "tokens": tup[3],
                "datediff": tup[4]
            }

            output.append(dict)

        return Response(content=json.dumps(output), status_code=200)
    
    def api_add_keys(self, tokens:int, days:int, num:int, manager_uid:str):
        output = []
        for i in range(num):
            uid = str(uuid.uuid4())
            self.nlp_sql_plugin.insert_by_union_id(uid)

            self.nlp_sql_plugin.update_by_union_id(uid, "ManagerUid", "'{}'".format(manager_uid))
            self.nlp_sql_plugin.update_by_union_id(uid, "Tokens", "{}".format(tokens))
            self.nlp_sql_plugin.update_by_union_id(uid, "ExpiryDate", "DATE(DATE('now'), '+{} days')".format(days))
            output.append(uid)

        return Response(content=json.dumps(output), status_code=200)
