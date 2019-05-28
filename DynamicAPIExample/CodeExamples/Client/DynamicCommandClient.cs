using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DynamicCommand
{
    public class DynamicCommandClient
    {
        private string _accessToken;
        private readonly string _identityUrl;
        private readonly string _commandUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly HttpClient _client;
        private readonly string _base64ClientCreds;

        private static ServiceProvider ServiceProvider { get; } = GetServiceProvider();

        private static IHttpClientFactory GetHttpClientFactory()
        {
            var httpClientFactory = ServiceProvider.GetService<IHttpClientFactory>();
            return httpClientFactory;
        }

        private static ServiceProvider GetServiceProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(cfg =>
            {
                cfg.SetMinimumLevel(LogLevel.Trace);
                cfg.AddProvider(new NunitTestContextLoggerProvider());
            });

            serviceCollection.AddTransient<CustomLoggingScopeHttpMessageHandler>();

            serviceCollection.AddHttpClient("CustomClient", cfg =>
            {
                cfg.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                cfg.Timeout = TimeSpan.FromMinutes(5);
            }).AddHttpMessageHandler<CustomLoggingScopeHttpMessageHandler>();

            return serviceCollection.BuildServiceProvider();
        }


        private IHttpClientFactory HttpClientFactory { get; } = GetHttpClientFactory();

        public DynamicCommandClient(string webServerUrl, string clientId, string clientSecret, string username, string password)
        {
            _identityUrl = $"{webServerUrl}/Identity/";
            _commandUrl = $"{webServerUrl}/API/";
            _username = username;
            _password = password;

            Encoding standardEncoding = Encoding.GetEncoding("iso-8859-1");
            byte[] clientCredsBytes = standardEncoding.GetBytes($"{clientId}:{clientSecret}");
            _base64ClientCreds = Convert.ToBase64String(clientCredsBytes);
            _client = HttpClientFactory.CreateClient("CustomClient");
            _client.BaseAddress = new Uri(_commandUrl);
        }


        private async Task Authenticate()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(_identityUrl),
                DefaultRequestHeaders =
                {
                    Authorization = new AuthenticationHeaderValue("Basic", _base64ClientCreds),
                    Accept = { new MediaTypeWithQualityHeaderValue("application/json") }
                }
            };

            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = _username,
                ["password"] = _password,
                ["scope"] = "basic_api"
            });

            var tokenResponse = await client.PostAsync("connect/token", formContent);
            if (tokenResponse.StatusCode == HttpStatusCode.OK)
            {
                //Received a new token
                var responseJson = await tokenResponse.Content.ReadAsAsync<JObject>();
                _accessToken = responseJson["access_token"].Value<string>();
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            else if (tokenResponse.StatusCode == HttpStatusCode.BadRequest)
            {
                //ExpectedApplicationErrors
                var responseJson = await tokenResponse.Content.ReadAsAsync<JObject>();
                throw new HttpRequestException($"400 Bad Request: {responseJson["error"].Value<string>()}");
            }
            else
            {
                //Everything else
                throw new HttpRequestException($"Response should either be 200 OK or 400 Bad Request. Received: {tokenResponse.StatusCode}");
            }
        }

        //Wrap built-in HTTP methods with expired token handling
        public async Task<HttpResponseMessage> GetApiAsync(string path, bool preAuthenticate = false)
        {
            if (preAuthenticate)
            {
                await Authenticate();
            }

            var response = await _client.GetAsync(path);
            if (!preAuthenticate && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await Authenticate();
                response = await _client.GetAsync(path);
            }
            return response;
        }

        public async Task<HttpResponseMessage> PostApiAsync<T>(string path, T content, bool preAuthenticate = false)
        {
            if (preAuthenticate)
            {
                await Authenticate();
            }

            var response = await _client.PostAsJsonAsync(path, content);
            if (!preAuthenticate && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await Authenticate();
                response = await _client.PostAsJsonAsync(path, content);
            }
            return response;
        }

        public async Task<HttpResponseMessage> PutApiAsync<T>(string path, T content, bool preAuthenticate = false)
        {
            if (preAuthenticate)
            {
                await Authenticate();
            }

            var response = await _client.PutAsJsonAsync(path, content);
            if (!preAuthenticate && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await Authenticate();
                response = await _client.PutAsJsonAsync(path, content);
            }
            return response;
        }

        public async Task<HttpResponseMessage> DeleteApiAsync(string path, bool preAuthenticate = false)
        {
            if (preAuthenticate)
            {
                await Authenticate();
            }

            var response = await _client.DeleteAsync(path);
            if (!preAuthenticate && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await Authenticate();
                response = await _client.DeleteAsync(path);
            }
            return response;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool preAuthenticate = false)
        {
            if (preAuthenticate)
            {
                await Authenticate();
            }

            var response = await _client.SendAsync(request);
            if (!preAuthenticate && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await Authenticate();
                response = await _client.SendAsync(request);
            }
            return response;
        }
    }
}