using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Samba.Modules.CreditCardModule.FirstData
{
    public class FdGatewayManager
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gatewayUri"></param>
        /// <param name="version"></param>
        public FdGatewayManager(string gatewayUri, ApiVersion version)
        {
            GatewayUri = gatewayUri;
            Version = version;
        }
        /// <summary>
        /// Gateway Uri
        /// </summary>
        public string GatewayUri { get; private set; }
        /// <summary>
        /// Api Version
        /// </summary>
        public ApiVersion Version { get; private set; }
        /// <summary>
        /// SendFdCreditCardRequest
        /// </summary>
        /// <param name="req"></param>
        /// <param name="resp"></param>
        /// <param name="errorResp"></param>
        /// <returns></returns>
        public bool SendFdCreditCardRequest(FdCreditCardReq req, out FdCreditCardResp resp, out string errorResp)
        {

            errorResp = "";
            resp = null;
            string result = String.Empty;
            string jsonPayload = String.Empty;

            try
            {
                jsonPayload = JsonConvert.SerializeObject(req);
            }
            catch (Exception ex)
            {
                errorResp = ex.Message;
                return false;
            }

            if (Version != ApiVersion.V11)
            {
                throw new Exception(String.Format("Version {0} is not supported", Version));
            }
            if (SendRequestV11(jsonPayload, out result))
            {
                try
                {
                    resp = JsonConvert.DeserializeObject<FdCreditCardResp>(result);
                    return true;
                }
                catch (Exception exception)
                {
                    try
                    {
                        JObject jobj = JObject.Parse(result);

                        resp = new FdCreditCardResp();
                        resp.transaction_approved = (jobj["transaction_approved"].ToString() == "1") ? true : false;
                        resp.bank_message = jobj["bank_message"].ToString();
                        resp.exact_resp_code = jobj["exact_resp_code"].ToString();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        errorResp = exception.Message;
                        return false;
                    }

                }
            }
            else
            {
                errorResp = result;
                return false;
            }

        }


        private bool SendRequestV11(string payload, out string result)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(GatewayUri);
                req.Method = "POST";
                req.ContentType = "application/json; charset=utf-8;";
                req.Accept = "application/json";
                req.Proxy = null;
                req.ServicePoint.Expect100Continue = false;
                req.Timeout = 15000;
                req.KeepAlive = true;
                var byteArray = Encoding.UTF8.GetBytes(payload);
                req.ContentLength = byteArray.Length;
                using (var dataStream = req.GetRequestStream())
                {
                    // Write the data to the request stream.
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    // Close the Stream object.
                    dataStream.Close();
                }

                using (var response = req.GetResponse())
                {
                  //  var requestStatus = ((HttpWebResponse) response).StatusDescription;
                    using (var responsedataStream = response.GetResponseStream())
                    {
                        if (responsedataStream == null)
                        {
                            result = "Failed to receive response from the server";
                            return false; 
                        }
                        using (var reader = new StreamReader(responsedataStream))
                        {
                            // Read the content.
                            result = reader.ReadToEnd();
                            reader.Close();
                            responsedataStream.Close();
                            response.Close();
                            return true;
                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
                return false;
            }

        }
        /// <summary>
        /// API Version
        /// </summary>
        public enum ApiVersion
        {
            V11,
            V12
        };

    }
}
