using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace api
{
    public static class TodoApi
    {
        private static readonly List<TodoItem> _todos = new List<TodoItem>();
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        static TodoApi()
        {
            // 文字エンコーディングの設定
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private static IActionResult CreateResponse(object content, HttpRequest req)
        {
            // レスポンスヘッダーの設定
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            // UTF-8でJSONシリアライズ
            var utf8WithoutBom = new UTF8Encoding(false);
            var json = JsonConvert.SerializeObject(content, _jsonSettings);
            var jsonBytes = utf8WithoutBom.GetBytes(json);
            
            return new FileContentResult(jsonBytes, "application/json; charset=utf-8");
        }

        [FunctionName("GetTodos")]
        public static IActionResult GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todos/list")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting all todos");
            
            var json = JsonConvert.SerializeObject(_todos);
            var utf8WithoutBom = new UTF8Encoding(false);
            var jsonBytes = utf8WithoutBom.GetBytes(json);
            
            req.HttpContext.Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            return new FileContentResult(jsonBytes, "application/json; charset=utf-8");
        }

        [FunctionName("CreateTodo")]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todos/create")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Creating a new todo");

            // リクエストボディをUTF-8で読み込み
            using var reader = new StreamReader(req.Body, new UTF8Encoding(false));
            string requestBody = await reader.ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoItem>(requestBody);
            
            var todo = new TodoItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = input.Title,
                IsCompleted = false
            };

            _todos.Add(todo);

            var json = JsonConvert.SerializeObject(todo);
            var utf8WithoutBom = new UTF8Encoding(false);
            var jsonBytes = utf8WithoutBom.GetBytes(json);
            
            req.HttpContext.Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            return new FileContentResult(jsonBytes, "application/json; charset=utf-8");
        }
    }
} 