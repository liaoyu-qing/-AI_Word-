import sqlite3

class SqlPlugin:
    def __init__(self, db, table_name) -> None:
        self.connect = sqlite3.connect(db, check_same_thread=False)
        self.cursor = self.connect.cursor()
        self.table_name = table_name

    def create_table(self, columns): # id INTEGER PRIMARY KEY, UnionId TEXT, UsedTokens INTEGER, UsedPromptTokens INTEGER, Tokens INTEGER, ExpiryDate DATE
        self.cursor.execute("CREATE TABLE IF NOT EXISTS {}({})".format(self.table_name, columns))
        self.connect.commit()

    def query_by_union_id(self, union_id, columns):
        self.cursor.execute('SELECT {} FROM {} WHERE UnionId=\'{}\''.format(columns, self.table_name, union_id))
        self.connect.commit()
        result = self.cursor.fetchone()
        return result
    
    def update_by_union_id(self, union_id:str, columns:str, value:str):
        self.cursor.execute('UPDATE {} SET {}={} WHERE UnionId=\'{}\''.format(self.table_name, columns, value, union_id))
        self.connect.commit()

    def insert_by_union_id(self, union_id):
        self.cursor.execute('INSERT INTO {} (UnionId) VALUES (\'{}\')'.format(self.table_name, union_id))
        self.connect.commit()

    def query_by_key(self, key: str, value:str, column_name:str):
        self.cursor.execute('SELECT {} FROM {} WHERE {}=\'{}\''.format(column_name, self.table_name, key, value))
        self.connect.commit()
        result = self.cursor.fetchall()
        # self.lock.release()
        return result