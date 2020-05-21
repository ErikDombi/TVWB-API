using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using PushSharp.Apple;
using RestSharp;
using RestSharp.Authenticators;
using Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace TVWBAPI
{
    public class NotificationHandler
    {
        ApnsConfiguration config;
        ApnsServiceBroker apnsBroker;
        public NotificationHandler()
        {
			config = new ApnsConfiguration(ApnsConfiguration.ApnsServerEnvironment.Sandbox, "AuthKey.p12", "torchman");
            apnsBroker = new ApnsServiceBroker(config);

			apnsBroker.OnNotificationFailed += (notification, aggregateEx) => {

				aggregateEx.Handle(ex => {

					// See what kind of exception it was to further diagnose
					if (ex is ApnsNotificationException notificationException)
					{

						// Deal with the failed notification
						var apnsNotification = notificationException.Notification;
						var statusCode = notificationException.ErrorStatusCode;

						Console.WriteLine($"Apple Notification Failed: ID={apnsNotification.Identifier}, Code={statusCode}");

					}
					else
					{
						// Inner exception might hold more useful information like an ApnsConnectionException			
						Console.WriteLine($"Apple Notification Failed for some unknown reason : {ex.InnerException}");
					}

					// Mark it as handled
					return true;
				});
			};

			apnsBroker.OnNotificationSucceeded += (notification) => {
				Console.WriteLine("Apple Notification Sent!");
			};

			var fbs = new FeedbackService(config);
			fbs.FeedbackReceived += (string deviceToken, DateTime timestamp) => {
				// Remove the deviceToken from your database
				// timestamp is the time the token was reported as expired
				Console.WriteLine("[APNS EXPIRED] " + deviceToken + " EXPIRED: " + timestamp.ToString());
			};
			fbs.Check();

			apnsBroker.Start();
		}

        public void sendAlert(string apnsid, string title, string subtitle, string body, string category)
        {
			apnsBroker.QueueNotification(new ApnsNotification
			{
				DeviceToken = apnsid,
				Payload = JObject.Parse("{\"aps\":{\"alert\":{\"title\": \"[[TITLE]]\",\"subtitle\":\"[[SUBTITLE]]\",\"body\":\"[[BODY]]\"},\"category\":\"[[CATEGORY]]\"}}".Replace("[[TITLE]]", title).Replace("[[SUBTITLE]]", subtitle).Replace("[[BODY]]", body).Replace("[[CATEGORY]]", category))
			});
        }
    }
}
