<template>
  <div class="container">
    <el-container>
      <el-aside width="200px">
        <div>
          <el-radio-group v-model="isCollapse" style="margin-bottom: 20px">
            <el-radio-button :value="false">展开</el-radio-button>
            <el-radio-button :value="true">关闭</el-radio-button>
          </el-radio-group>
          <el-menu
            default-active="1"
            class="el-menu-vertical-demo"
            :collapse="isCollapse"
            @open="handleOpen"
            @close="handleClose"
            @select="MenuChange"
          >
            <el-menu-item index="1">
              <el-icon><icon-menu /></el-icon>
              <template #title>查看KEY</template>
            </el-menu-item>
            <el-menu-item index="2">
              <el-icon><Promotion /></el-icon>
              <template #title>生成KEY</template>
            </el-menu-item>
            <el-menu-item index="3">
              <el-icon><WarnTriangleFilled /></el-icon>
              <template #title>过期KEY</template>
            </el-menu-item>
            <el-menu-item index="4">
              <el-icon><setting /></el-icon>
              <template #title>设置</template>
            </el-menu-item>
          </el-menu>
          <div></div>
        </div>
      </el-aside>
      <el-main>
        <el-table
          :data="tableData.apiArr"
          style="width: 100%"
          v-show="active == '1'"
        >
          <el-table-column prop="key" label="API_KEY" align="center" />
          <el-table-column
            prop="used_tokens"
            label="已用tokens"
            align="center"
          />
          <el-table-column prop="remainApi" label="剩余tokens" align="center" />
          <el-table-column prop="datediff" label="有效期" align="center" />
        </el-table>
        <div class="page2" v-show="active == '2'">
          <div class="inputClass">
            <div class="title">token时长（天）:</div>
            <el-input
              min="0"
              v-model="add_token.tokenDate"
              type="number"
              style="width: 240px"
              placeholder=""
            />
          </div>
          <div class="inputClass">
            <div class="title">token数量:</div>
            <el-input
              min="0"
              v-model="add_token.tokenNum"
              type="number"
              style="width: 240px"
              placeholder=""
            />
          </div>
          <div class="inputClass">
            <div class="title">生成数量:</div>
            <el-input
              min="0"
              v-model="add_token.num"
              type="number"
              style="width: 240px"
              placeholder=""
            />
          </div>
          <div class="confire_btn">
            <el-button type="primary" @click="add_tokenButton">确认</el-button>
          </div>
        </div>
        <el-table
          :data="overTableData.apiArr"
          style="width: 100%"
          v-show="active == '3'"
        >
          <el-table-column prop="key" label="API_KEY" align="center" />
          <el-table-column
            prop="used_tokens"
            label="已用tokens"
            align="center"
          />
          <el-table-column prop="remainApi" label="剩余tokens" align="center" />
          <el-table-column prop="datediff" label="有效期" align="center" />
        </el-table>
        <div class="seting" v-show="active == '4'">
          <div class="page2">
            <div class="inputClass">
              <div class="title">设置请求URL:</div>
              <el-input
                v-model="requestUrl"
                style="width: 240px"
                placeholder=""
              />
            </div>

            <div class="confire_btn">
              <el-button type="primary" @click="seting">确认</el-button>
            </div>
          </div>
        </div>
      </el-main>
    </el-container>
  </div>
</template>

<script lang="ts" setup>
import { onMounted, reactive, ref } from "vue";
import instance from "../axios";
import {
  Document,
  Menu as IconMenu,
  Location,
  Menu,
  Setting,
} from "@element-plus/icons-vue";
import axios from "axios";
const active = ref("1");
const isCollapse = ref(false);
const add_token = ref({
  tokenDate: "",
  tokenNum: "",
  num: "",
});
const requestUrl = ref("");
let showTableData = ref({
  apiArr: null,
});
let tableData = ref({
  apiArr: null,
});
let overTableData = ref({
  apiArr: null,
});
// const arr = ref([])
onMounted(() => {
  queryRequestUrl()
  get_Key_Msg();
});
const queryOverKey = () => {};
const get_Key_Msg = () => {
  instance({
    url: "/api/v1/manager/query_keys",
    params: {
      manager_uid: "liaoyuAi",
    },
  })
    .then((res) => {
      console.log(res);
      let arr = [];
      let overArr = [];
      for (let i = 0; i < res.length; i++) {
        if (res[i].datediff < 0) {
          res[i].datediff = 0;
        }
        res[i].remainApi = res[i].tokens;
        if (res[i].datediff > 0) {
          arr.push(res[i]);
        } else {
          overArr.push(res[i]);
        }

        if (i == res.length - 1) {
          console.log(arr);
          overTableData.value.apiArr = overArr;
          tableData.value.apiArr = arr;
          // showTableData.value.apiArr = tableData.value;
        }
      }
      // let arr=res
      // tableData.value.apiArr=res
    })
    .catch((err) => {
      console.log(err);
    });
  // axios.get("http://192.168.0.100:3001/api/v1/manager/query_keys", {
  //   params: {
  //     manager_uid: "liaoyuAi",
  //   },
  // })
};
const add_tokenApi = () => {
  instance({
    url: "/api/v1/manager/add_keys",
    params: {
      tokens: add_token.value.tokenNum,
      days: add_token.value.tokenDate,
      num: add_token.value.num,
      manager_uid: "liaoyuAi",
    },
  })
    .then((res) => {
      get_Key_Msg();
      console.log(res);
    })
    .catch((err) => {
      console.log(err);
    });
};
const add_tokenButton = () => {
  if (
    add_token.value.tokenDate !== "" &&
    add_token.value.tokenNum !== "" &&
    add_token.value.num !== ""
  ) {
    add_tokenApi();
  } else {
    msgopen();
  }
};
const queryRequestUrl =()=>{
  const invalid = localStorage.getItem('Url');
  if(!invalid){
     ElMessageBox.alert("请先设置请求的URL地址如（http://192.168.0.100:3001）", "提示", {
      // if you want to disable its autofocus
      // autofocus: false,
      confirmButtonText: "OK",
    });
    return
  }
}
const seting = () => {
  if (requestUrl.value == "") {
    ElMessageBox.alert("请输入完整内容", "提示", {
      // if you want to disable its autofocus
      // autofocus: false,
      confirmButtonText: "OK",
    });
  } else {
    localStorage.setItem("Url", requestUrl.value);
  }
  location.reload()
};
const handleOpen = (key: string, keyPath: string[]) => {
  console.log(key, keyPath);
};
const handleClose = (key: string, keyPath: string[]) => {
  console.log(key, keyPath);
};
const MenuChange = (e) => {
  active.value = e + "";
  console.log(active.value);
};
import { ElMessage, ElMessageBox } from "element-plus";
import type { Action } from "element-plus";

const msgopen = () => {
  ElMessageBox.alert("请输入完整内容", "提示", {
    // if you want to disable its autofocus
    // autofocus: false,
    confirmButtonText: "OK",
  });
};
</script>

<style lang="less">
.el-menu-vertical-demo:not(.el-menu--collapse) {
  width: 200px;
  min-height: 400px;
}
.page2 {
  width: 400px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  .inputClass {
    color: #409eff;
    margin-top: 20px;
    .title {
      height: 30px;
      line-height: 30px;
      margin-right: 20px;
    }
  }
  .confire_btn {
    margin-top: 100px;
    // margin-left: 20px;
    width: 240px;
    display: flex;
    justify-content: center;
    align-items: center;
  }
}
</style>