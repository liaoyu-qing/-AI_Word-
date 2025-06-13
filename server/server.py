from api import ApiConfig, Api
from manager_api import ManagerApi
from fastapi import FastAPI
from multiprocessing import Process
import argparse
parser = argparse.ArgumentParser(description='manual to this script')
parser.add_argument("--port", type=int, default="3000")
parser.add_argument("--host", type=str, default="0.0.0.0")
args = parser.parse_args()

def main_proc():
    app = FastAPI()
    api_config = ApiConfig()
    api_config.host = args.host
    api_config.port = args.port
    api = Api(app, api_config)
    api.launch()

def manager_proc():
    manager_app = FastAPI()
    manager_api_config = ApiConfig()
    manager_api_config.host = '127.0.0.1'
    manager_api_config.port = 3001
    manager_api = ManagerApi(manager_app, manager_api_config)
    manager_api.launch()

if __name__ == "__main__":

    mp = Process(target=manager_proc)
    mp.start()

    main_proc()