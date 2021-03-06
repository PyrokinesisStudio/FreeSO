﻿using FSO.Common.Utils;
using FSO.Server.Clients.Framework;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Clients
{
    public class ApiClient : AbstractHttpClient
    {
        private RestClient client;
        public static string CDNUrl;

        public static string AuthKey;

        public ApiClient(string baseUrl) : base(baseUrl) {
            client = Client();
        }

        public void GetThumbnailAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/" + shardID + "/" + location + ".png");

            client.ExecuteAsync(request, (resp, h) =>
            {
                GameThread.NextUpdate(x =>
                {
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                        callback(resp.RawBytes);
                });
            });
        }

        public void AdminLogin(string username, string password, Action<bool> callback)
        {
            var client = Client();
            var request = new RestRequest("admin/oauth/token", Method.POST);
            request.AddParameter("application/x-www-form-urlencoded",
                "grant_type=password&username="+username+"&password="+password,
                ParameterType.RequestBody);

            client.ExecuteAsync(request, (resp, h) =>
            {
                var ok = resp.StatusCode == System.Net.HttpStatusCode.OK;
                if (ok) { 
                    dynamic obj = JsonConvert.DeserializeObject(resp.Content);
                    AuthKey = obj.access_token;
                }
                GameThread.NextUpdate(x =>
                {
                    callback(ok);
                });
            });
        }

        public void GetFSOV(uint shardID, uint lotLocation, Action<byte[]> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/"+shardID+"/"+lotLocation+".fsov", Method.GET);
            request.AddHeader("authorization", "bearer " + AuthKey);

            client.ExecuteAsync(request, (resp, h) =>
            {
                var ok = resp.StatusCode == System.Net.HttpStatusCode.OK;
                byte[] dat = resp.RawBytes;
                GameThread.NextUpdate(x =>
                {
                    callback(ok?dat:null);
                });
            });
        }

        public void UploadFSOF(uint shardID, uint lotLocation, byte[] data, Action<bool> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/" + shardID + "/uploadfacade/" + lotLocation, Method.POST);
            request.AddFile("application/octet-stream", data, lotLocation + ".fsof");
            request.AddHeader("authorization", "bearer " + AuthKey);

            client.ExecuteAsync(request, (resp, h) =>
            {
                var ok = resp.StatusCode == System.Net.HttpStatusCode.OK;
                GameThread.NextUpdate(x =>
                {
                    callback(ok);
                });
            });
        }

        public void GetLotList(uint shardID, Action<uint[]> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/" + shardID + "/city.json");

            client.ExecuteAsync(request, (resp, h) =>
            {
                GameThread.NextUpdate(x =>
                {
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                    {
                        dynamic obj = JsonConvert.DeserializeObject(resp.Content);
                        Newtonsoft.Json.Linq.JArray data = obj.reservedLots;
                        uint[] result = data.Select(y => Convert.ToUInt32(y)).ToArray();
                        callback(result);
                    }
                });
            });
        }
    }
}
