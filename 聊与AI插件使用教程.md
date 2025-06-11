## 聊与AI后台管理前端

### 安装教程

技术栈：Vite+Vue3+elementui Plus

```
#需要提前准备node环境，最低版本 v14.20
#进入项目目录
cd 聊与AI后台管理服务端

# 安装依赖
npm install -registry=https://registry.npmmirror.com/
# 启动项目
npm run dev

# 构建生产环境
npm run build
```

## 聊与AI后台管理服务端

### 安装教程

```
conda create -n wai python=3.10
conda activate wai
# 切换到服务端目录
pip install -r requirements.txt
python server.py

# 配置大模型API_KEY
打开config.json填入对应在各个大模型官网注册的API_KEY
```

## 聊与AI插件

### 安装教程

```
# 开发软件:Microsoft Visual Studio
# 进入 插件代码\聊与AI
# 打开聊与AI.sln导出项目
# 点击生成/发布聊与AI
# 进入生成的文件夹启动安装程序setup.exe
```

### 使用教程

```
进入word切换至聊与AI
点击KEY输入AIP_KEY（管理页面生成）
Url为本地部署后的服务器地址
扩选文本选择要使用的功能
```

### 联系我们

邮箱：liaoyufurunqing@gmail.com

地址：河南省郑州市郑东新区福禄路98号海赋国际1128

![image-20250611143323704](C:\Users\QingJicheng\AppData\Roaming\Typora\typora-user-images\image-20250611143323704.png)