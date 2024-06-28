using Opc.Ua;
using Opc.Ua.Client;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpcUa
{
    internal class OpcUaClient
    {
        /// <summary>
        /// 전역변수
        /// </summary>
        private ApplicationConfiguration config;
        public Session session;
        private Subscription subscription;

        // 이벤트 처리
        public delegate void GetData(string tag, string value);
        public event GetData getData;

        /// <summary>
        /// OPC UA - 시작
        /// </summary>
        public async Task StartOpcua(string endPointUrl, string[] tagList)
        {
            // 설정 정보 생성
            config = CreateOpcUaAppConfiguration();

            // 세션 생성
            session = await Session.Create(config, new ConfiguredEndpoint(null, new EndpointDescription(endPointUrl)), true, "", 60000, null, null);

            // 연결 유지
            session.KeepAlive += Session_KeepAlive;

            // 구독 생성
            subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 1000, PublishingEnabled = true };

            // 구독 아이템(TAG) 정의
            var list = new List<MonitoredItem> { };

            // 데이터 바인딩
            foreach (string row in tagList)
            {
                list.Add(new MonitoredItem(subscription.DefaultItem) { DisplayName = row, StartNodeId = "ns=2;s=" + row });
            }

            // 구독 아이템 추가
            list.ForEach(i => i.Notification += new MonitoredItemNotificationEventHandler(OnNotification));
            subscription.AddItems(list);

            // 구독 추가
            session.AddSubscription(subscription);
            subscription.Create();
        }

        /// <summary>
        /// 연결 유지
        /// </summary>
        private void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                Console.WriteLine($"{e.Status} {session.OutstandingRequestCount}/{session.DefunctRequestCount}");
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// OPC UA - 구독
        /// </summary>
        private void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            MonitoredItemNotification notification = e.NotificationValue as MonitoredItemNotification;

            foreach (var tagValue in item.DequeueValues())
            {
                //decimal opcVal = 0;
                //decimal.TryParse(tagValue.Value.ToString(), out opcVal);
                // ★☆★☆★
                //Console.WriteLine($"{item.DisplayName} : {tagValue.Value.ToString()}");
                if (tagValue.Value != null)
                {
                    getData(item.DisplayName, tagValue.Value.ToString());
                }

            }
        }

        /// <summary>
        /// OPC UA - 설정
        /// </summary>
        private ApplicationConfiguration CreateOpcUaAppConfiguration()
        {
            var config = new ApplicationConfiguration()
            {
                ApplicationName = "OpcUaClient",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier(),
                    AutoAcceptUntrustedCertificates = true
                },
                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 10000
                }
            };

            config.Validate(ApplicationType.Client);

            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (s, e) =>
                {
                    e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
                };
            }

            return config;
        }
    }
}


