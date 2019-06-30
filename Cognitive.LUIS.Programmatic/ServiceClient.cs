﻿using Cognitive.LUIS.Programmatic.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Cognitive.LUIS.Programmatic
{
    public class ServiceClient : IDisposable
    {
        private readonly HttpClient _client;

        public ServiceClient(string subscriptionKey, Regions region)
        {
            var baseUrl = $"https://{region.ToString().ToLower()}.api.cognitive.microsoft.com/luis/api/v2.0/";
            _client = HttpClientFactory.Create(baseUrl, subscriptionKey);
        }

        protected async Task<string> Get(string path)
        {
            var response = await _client.GetAsync(path);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return responseContent;
            else if (response.StatusCode != System.Net.HttpStatusCode.BadRequest)
            {
                var exception = JsonConvert.DeserializeObject<ServiceException>(responseContent);
                var errorMessage = exception.Message;
                
                if (exception.Error != null)
                    errorMessage = $"{exception.Error.Code} - {exception.Error.Message}";
                
                throw new Exception(errorMessage);
            }
            return null;
        }

        protected async Task<string> Post(string path)
        {
            var response = await _client.PostAsync(path, null);
            return await GetResponseContent(response);
        }

        protected async Task<string> Post<TRequest>(string path, TRequest requestBody)
        {
            using (var content = new ByteArrayContent(GetByteData(requestBody)))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await _client.PostAsync(path, content);
                return await GetResponseContent(response);
            }
        }

        protected async Task Put<TRequest>(string path, TRequest requestBody)
        {
            using (var content = new ByteArrayContent(GetByteData(requestBody)))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await _client.PutAsync(path, content);
                await GetResponseContent(response);
            }
        }

        protected async Task Delete(string path)
        {
            var response = await _client.DeleteAsync(path);
            await GetResponseContent(response);
        }

        private async Task<string> GetResponseContent(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var exception = JsonConvert.DeserializeObject<ServiceException>(responseContent);
                throw new Exception(exception.ToString());
            }
            return responseContent;
        }

        private byte[] GetByteData<TRequest>(TRequest requestBody)
        {
            var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var body = JsonConvert.SerializeObject(requestBody, settings);
            return Encoding.UTF8.GetBytes(body);
        }

        public void Dispose() =>
            _client.Dispose();
    }
}
