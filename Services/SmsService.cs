using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace SmartRecyclingRewardsSystem.Services
{
    public class SmsService
    {
        private readonly string _apiKey;
        private const string ClickatellUrl = "https://platform.clickatell.com/messages/http/send";

        public SmsService()
        {
            _apiKey = WebConfigurationManager.AppSettings["ClickatellApiKey"];
        }

        public async Task<bool> SendSmsAsync(string toPhoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) return false;
            if (string.IsNullOrWhiteSpace(toPhoneNumber)) return false;

            try
            {
                var phone = toPhoneNumber.Replace(" ", "").Replace("-", "");
                if (phone.StartsWith("0"))
                    phone = "+27" + phone.Substring(1);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);
                    var url = string.Format("{0}?to={1}&content={2}",
                        ClickatellUrl, Uri.EscapeDataString(phone), Uri.EscapeDataString(message));

                    var response = await client.GetAsync(url);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
