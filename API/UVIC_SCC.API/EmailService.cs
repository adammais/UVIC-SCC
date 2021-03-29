using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace UVIC_SCC.API
{
    public static class EmailService
    {
        [FunctionName("Email")]
        public static async Task<IActionResult> Run(
                            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
                            ILogger log)
        {
            log.LogInformation("Email() fired.");

            // Parse parameters
            string from = null;
            string subject = null;
            string message = null;
            string responseMessage = null;
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            from = req.Query["from"];
            from = from ?? data?.from;

            subject = req.Query["subject"];
            subject = subject ?? data?.subject;

            message = req.Query["message"];
            message = message ?? data?.message;

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
            {
                responseMessage = "FROM, SUBJECT and MESSAGE must be sent in the query parameters or posted in the body.";
                return new BadRequestObjectResult(responseMessage);
            }
            else
            {
                Response emailResponse = await EmailMessageToSCC(from, subject, message);
                if (emailResponse.IsSuccessStatusCode)
                {
                    responseMessage = emailResponse.Body.ToString();
                    return new OkObjectResult(responseMessage);
                }
                else
                {
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
        }

        [FunctionName("Ping")]
        public static async Task<IActionResult> Ping([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
                            ILogger log)
        {
            await Task.Run(() => log.LogInformation("UVIC_SCC_API.EmailService.Ping() fired..."));
            return new OkObjectResult("UVIC_SCC_API.EmailService.Ping() fired...");
        }

        private static async Task<Response> EmailMessageToSCC(string fromInformation, string subject, string message)
        {
            var apiKey = Environment.GetEnvironmentVariable("APPSETTING_SENDGRID_API_KEY"); //insert your Sendgrid API Key
            var toAddress = Environment.GetEnvironmentVariable("APPSETTING_TO_ADDRESS");
            string toDescriptor = "UVIC SCC President";
            var fromEmailAddress = Environment.GetEnvironmentVariable("APPSETTING_FROM_ADDRESS");
            string fromDescriptor = "UVIC SCC Mail Form";

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmailAddress, fromDescriptor);
            var to = new EmailAddress(toAddress, toDescriptor);
            var plainTextContent = subject + "\n" + message + "\n" + fromInformation;
            var htmlContent = "<strong>UVIC SCC Mail Form Submission</strong><br/>" + subject + "<br/>" + message + "<br/>" + fromInformation;
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            return response;
        }
    }
}

