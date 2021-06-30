using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;

namespace Xrader.lib
{ 
    public class UpbitController
    {
        public event Action<string> UpbitPriceCallback;
        private WebSocket webSocket;

        private string account_url = "https://api.upbit.com/v1/accounts";
        private string order_url = "https://api.upbit.com/v1/orders";
        private string cancel_order_url = "https://api.upbit.com/v1/order";
        private string chance_url = "https://api.upbit.com/v1/orders/chance";
        private string symbol_list_url = "https://api.upbit.com/v1/market/all";
        private string check_order_url = "https://api.upbit.com/v1/order";
        private string UUID = Guid.NewGuid().ToString();
        private string AccessKey = ""; //발급받은 AccessKey를 넣어줍니다.
        private string SecretKey = ""; //발급받은 SecretKey를 넣어줍니다.
        private IJwtEncoder encoder;

        public UpbitController(string access, string secret)
        {
            this.AccessKey = access;
            this.SecretKey = secret;

            IJwtAlgorithm algorithm = new HMACSHA256Algorithm(); //JWT 라이브러리 이용하여 JWT 토큰을 만듭니다.
            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            encoder = new JwtEncoder(algorithm, serializer, urlEncoder);
        }

        #region Utility
        private string QueryString(IDictionary<string, object> dict)
        {
            var list = new List<string>();
            foreach (var item in dict)
            {
                list.Add(item.Key + "=" + item.Value);
            }
            return string.Join("&", list);
        }

        private string SHA512Hash(string data)
        {
            SHA512 sha512 = SHA512.Create();
            byte[] queryHashByteArray = sha512.ComputeHash(Encoding.UTF8.GetBytes(data));
            string queryHash = BitConverter.ToString(queryHashByteArray).Replace("-", "").ToLower();
            return queryHash;
        }
        #endregion

        public string AccountInquiry() //나의 계좌 조회 (전체 계좌 조회)
        {
            var payload = new Dictionary<string, object>
            {
                { "access_key" , AccessKey },
                { "nonce" , UUID },
            };
            
            var token = encoder.Encode(payload, SecretKey);
            var authorize_token = string.Format("Bearer {0}", token);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(account_url); //요청 과정
            request.Method = "GET";
            request.Headers.Add(string.Format("Authorization:{0}", authorize_token));
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string strResult = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            return strResult;
        }
        public string GetSymbolList() // 전체 종목 조회
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(symbol_list_url); //요청 과정
            request.Method = "GET";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string strResult = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            return strResult;
        }

        public string AcoountPossibleOrderList() // 주문가능 목록 조회
        {
            Dictionary<string, object> query = new Dictionary<string, object>
            {
                { "market" , "KRW-BTC" }
            };

            string query_string = QueryString(query);

            var payload = new Dictionary<string, object>
            {
                { "access_key" , AccessKey },
                { "nonce" , Guid.NewGuid().ToString() },
                { "query_hash", SHA512Hash(query_string) },
                { "query_hash_alg", "SHA512" }
            };

            var token = encoder.Encode(payload, SecretKey);
            var authorize_token = string.Format("Bearer {0}", token);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(chance_url + "?market=KRW-BTC"); //요청 과정
            request.Method = "GET";
            request.Headers.Add(string.Format("Authorization:{0}", authorize_token));
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string strResult = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            return strResult;
        }

        public string CheckOrder(string order_uuid, string identifier="")
        {
            Dictionary<string, object> query = new Dictionary<string, object>
            {
                { "uuid" , order_uuid }
            };

            string query_string = QueryString(query);
            var payload = new Dictionary<string, object>
            {
                { "access_key" , AccessKey },
                { "nonce" , Guid.NewGuid().ToString() },
                { "query_hash", SHA512Hash(query_string) },
                { "query_hash_alg", "SHA512" }
            };

            var token = encoder.Encode(payload, SecretKey);
            var authorize_token = string.Format("Bearer {0}", token);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(check_order_url + "?uuid=" + order_uuid); //요청 과정
            request.Method = "GET";
            request.Headers.Add(string.Format("Authorization:{0}", authorize_token));
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string strResult = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            return strResult;
        }

        public string CancelOrder(string uuid)
        {
            Dictionary<string, object> query = new Dictionary<string, object>
            {
                { "uuid" , uuid },
            };

            string query_string = QueryString(query);

            var payload = new JwtPayload
            {
                { "access_key" , AccessKey },
                { "nonce" , Guid.NewGuid().ToString() },
                { "query_hash", SHA512Hash(query_string) },
                { "query_hash_alg", "SHA512" }
            };
            var token = encoder.Encode(payload, SecretKey);
            var authorizationToken = string.Format("Bearer {0}", token);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(cancel_order_url + "?" + query_string); //요청 과정
            //Console.WriteLine(order_url + "?" + query_string);
            request.Method = "DELETE";
            request.ContentType = "application/json";
            request.Headers.Add(string.Format("Authorization:{0}", authorizationToken));
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string strResult = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            return strResult;
        }

        public string Order(string CoinName, string Side, double Balance, double Price, string OrdType)
        {
            Dictionary<string, object> query = new Dictionary<string, object>
            {
                { "market" , CoinName },
                { "side" , Side }, // 매도 ask 매수 bid
                { "ord_type" , OrdType } // limit : 지정가, price : 시장가 매수, market : 시장가 매도
            };

            if (Side == "ask")
            {
                if (OrdType == "limit")
                    query["price"] = Price;
                query["volume"] = Balance;
            }
            else
                query["price"] = Price;
                //query["volume"] = Balance;

            string query_string = QueryString(query);

            var payload = new JwtPayload
            {
                { "access_key" , AccessKey },
                { "nonce" , Guid.NewGuid().ToString() },
                { "query_hash", SHA512Hash(query_string) },
                { "query_hash_alg", "SHA512" }
            };
            var token = encoder.Encode(payload, SecretKey);
            var authorizationToken = string.Format("Bearer {0}", token);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(order_url + "?" + query_string); //요청 과정
            //Console.WriteLine(order_url + "?" + query_string);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add(string.Format("Authorization:{0}", authorizationToken));
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string strResult = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            return strResult;
        }

        public void StartOrderBook(string CoinName, Action<string> callback)
        {
            UpbitPriceCallback = callback;
            JArray array = new JArray();
            array.Add(CoinName);
            //array.Add("KRW-XRP");

            JObject obj1 = new JObject();
            obj1["ticket"] = Guid.NewGuid();//UUID

            JObject obj2 = new JObject();
            obj2["type"] = "trade";//TypeEnums.UpbitDataType.trade.ToString();
            obj2["codes"] = array;

            JObject obj3 = new JObject();
            obj3["type"] = "orderbook";//TypeEnums.UpbitDataType.orderbook.ToString();
            obj3["codes"] = array;

            //string sendMsg = string.Format("[{0},{1},{2}]", obj1.ToString(), obj2.ToString(), obj3.ToString());
            string sendMsg = string.Format("[{0},{1}]", obj1.ToString(), obj2.ToString());
            webSocket = new WebSocket("wss://api.upbit.com/websocket/v1");
            webSocket.OnMessage += Ws_OnMessage;
            webSocket.Connect();
            if (webSocket.ReadyState == WebSocketState.Open)
            {
                webSocket.Send(sendMsg);
            }


        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            string requestMsg = Encoding.UTF8.GetString(e.RawData);

            if (e.IsPing)
            {
                Console.WriteLine("*************   Recevie Ping Data");
                Console.WriteLine(requestMsg);
                webSocket.Send(requestMsg);
            }
            else
                //Console.WriteLine(requestMsg);
                UpbitPriceCallback(requestMsg);
        }




    }
}
