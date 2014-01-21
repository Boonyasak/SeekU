﻿using System;
using System.Configuration;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using SeekU.Commanding;

namespace SeekU.Azure.Commanding
{
    public class AzureCommandBus : ICommandBus
    {
        private static bool _queueCreated;

        public static string DefaultConnectionString;
        public string AzureServiceBusConnectionString { get; set; }

        public AzureCommandBus()
        {
            #region Default connection string
            try
            {
                DefaultConnectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"] ??
                                          ConfigurationManager.AppSettings["SeekU.AzureServiceBus.ConnectionString"];
            }
            catch
            {
                // No need to handle missing default value because it can be configured
                // by the DI container.
            }

            #endregion
        }

        public void Send<T>(T command) where T : ICommand
        {
            var connection = AzureServiceBusConnectionString ?? DefaultConnectionString;

            if (connection == null)
            {
                throw new ArgumentException(@"Azure command bus connection has not been configured.  Please update config file or Dependency Resolver.");
            }

            CreateQueue(connection);

            SendMessage(command, connection);
        }

        public virtual void SendMessage(ICommand command, string connection)
        {
            var message = new BrokeredMessage(command) { ContentType = command.GetType().AssemblyQualifiedName };
            var client = QueueClient.CreateFromConnectionString(connection, "Commands");
            client.Send(message);
        }

        public virtual void CreateQueue(string connection)
        {
            var manager = NamespaceManager.CreateFromConnectionString(connection);

            // Prevent re-entrancy for every command
            if (!_queueCreated)
            {
                if (!manager.QueueExists("Commands"))
                {
                    manager.CreateQueue(new QueueDescription("Commands"));
                }

                _queueCreated = true;
            }
        }
    }
}
