<template>
  <div class="container">
    <div class="field">
      <label for="passwordInput">Впишите пароль</label>
      <input v-model="passwordInput" placeholder="например: abcd">
      <button class="text-button" @click="createMD5">Захешировать и отправить на расшифровку</button>
      <label v-if="hashValue.length != 0" for="passwordInput">Вот такой хеш: {{ hashValue }}</label>
      
      <input v-model="hashInput" placeholder="md5">
      <button class="text-button" @click="sendHash">отправить на расшифровку</button>
    </div>
    
    <div v-if="requestId" class="field">
      <div>ID запроса: <strong>{{ requestId }}</strong></div>
      <button class="text-button" @click="getPassword">Проверить статус</button>
    </div>
    
    <div class="result-field">
      <h3>Результат:</h3>
      <div class="result-box">
        <pre>{{ resultDisplay }}</pre>
      </div>
    </div>
  </div>
</template>

<script setup>
  import axios from 'axios';  
  import * as CryptoJS from 'crypto-js'
  import { ref } from 'vue'
  
  const passwordInput = ref("");
  const hashInput = ref("");
  const requestId = ref("");
  const resultDisplay = ref("Ожидание ввода пароля...");
  const hashValue = ref("");

  const apiClient = axios.create({
    baseURL: 'http://localhost:8080/api',
    timeout: 5000,
    headers: {
      'Content-Type': 'application/json',
    }
  });

  async function createMD5() {
    if (!passwordInput.value) {
      resultDisplay.value = "Ошибка: введите пароль";
      return;
    }

    try {
      const hash = CryptoJS.MD5(passwordInput.value).toString();
      hashValue.value = hash;
      console.log('Отправляем хеш:', hash);
      
    } catch (error) {
      console.error('Ошибка:', error);
    }
  }

  async function sendHash(){
    const response = await apiClient.post('/hash/crack', {
        hash: hashInput.value,
        maxLength: passwordInput.value.length
      });
      
      console.log('Ответ сервера:', response.data);
      
      if (response.data) {
        requestId.value = response.data.RequestId;
        resultDisplay.value = `Запрос создан! ID: ${requestId.value}\nХеш: ${hashInput.value}\nОжидайте...`;
      } else {
        throw new Error('Некорректный ответ сервера');
      }
  }

  async function getPassword() {
    if (!requestId.value) {
      resultDisplay.value = "Сначала создайте запрос";
      return;
    }

    try {
      const response = await apiClient.get('/hash/status', {
        params: { requestId: requestId.value }
      });
      
      console.log('Статус ответ:', response.data);
      
      const status = response.data.Status;
      const data = response.data.Data;
      

      if (status === 'READY') {
        if (data && data.length > 0) {
          resultDisplay.value = `НАЙДЕН ПАРОЛЬ: "${data[0]}"\n\nВсе найденные варианты:\n${data.join('\n')}`;
        } else {
          resultDisplay.value = `Статус: READY, но пароль не найден (возможно, ошибка)`;
        }
      } else if (status === 'IN_PROGRESS') {
        resultDisplay.value = `Статус: IN_PROGRESS\nЗапрос еще обрабатывается...`;
      } else if (status === 'ERROR') {
        resultDisplay.value = `Статус: ERROR\nВремя ожидания истекло или произошла ошибка`;
      }  else if (status === 'PARTIALLY_COMPLETED') {
        if (data && data.length > 0) {
          resultDisplay.value = `Статус: PARTIALLY_COMPLETED НАЙДЕН ПАРОЛЬ: "${data[0]}"\n\nВсе найденные варианты:\n${data.join('\n')}`;
        } else {
          resultDisplay.value = `Статус: PARTIALLY_COMPLETED, но пароль не найден (возможно, ошибка)`;
        }
      } else {
        resultDisplay.value = `Статус: ${status}`;
      }
      
    } catch (error) {
      console.error('Ошибка:', error);
    }
  }
</script>

<style scoped>
.container {
  display: flex;
  margin: 30px;
  min-height: 100vh;
  flex-direction: column;
  gap: 20px;
  max-width: 800px;
}

.field {
  display: flex;
  flex-direction: row;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
}

.result-field {
  margin-top: 20px;
  border-top: 1px solid #ccc;
  padding-top: 20px;
}

.result-box {
  background: #f5f5f5;
  border-radius: 8px;
  padding: 15px;
  min-height: 100px;
  border: 1px solid #ddd;
  font-family: monospace;
}

.text-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 40px;
  padding: 0 20px;
  text-align: center;
  background: #3498db;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  transition: background 0.2s;
}

.text-button:hover {
  background: #2980b9;
}

.text-button:active {
  transform: translateY(1px);
}

input {
  height: 36px;
  padding: 0 10px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  min-width: 200px;
}

input:focus {
  outline: none;
  border-color: #3498db;
}

pre {
  margin: 0;
  white-space: pre-wrap;
  word-wrap: break-word;
}
</style>