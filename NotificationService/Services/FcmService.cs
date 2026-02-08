using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NotificationService.Services
{
    public interface IFcmService
    {
        Task SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
        Task SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);
    }

    public class FcmService : IFcmService
    {
        private readonly ILogger<FcmService> logger;
        private readonly IConfiguration configuration;
        private FirebaseMessaging messagingClient;
        private static readonly object initLock = new object();

        public FcmService(ILogger<FcmService> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            try
            {
                lock (initLock)
                {
                    FirebaseApp app = null;

                    // Попробуем получить уже созданный DefaultInstance
                    try
                    {
                        app = FirebaseApp.DefaultInstance;
                        logger.LogDebug("FirebaseApp.DefaultInstance found (Name={name})", app?.Name);
                    }
                    catch (InvalidOperationException)
                    {
                        logger.LogDebug("No FirebaseApp.DefaultInstance found; will create one.");
                    }

                    if (app == null)
                    {
                        var credentialsPath = configuration["Firebase:CredentialsPath"] ?? "firebase-credentials.json";

                        // Resolve relative path
                        if (!Path.IsPathRooted(credentialsPath))
                        {
                            credentialsPath = Path.Combine(AppContext.BaseDirectory, credentialsPath);
                        }

                        if (!File.Exists(credentialsPath))
                        {
                            var envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                            {
                                credentialsPath = envPath;
                                logger.LogInformation("Using GOOGLE_APPLICATION_CREDENTIALS at {path}", credentialsPath);
                            }
                            else
                            {
                                logger.LogWarning("Credentials file not found at {path}. Will attempt application default credentials.", credentialsPath);
                            }
                        }

                        var appOptions = new AppOptions();

                        if (File.Exists(credentialsPath))
                        {
                            logger.LogInformation("Loading credentials from file: {path}", credentialsPath);
                            appOptions.Credential = GoogleCredential.FromFile(credentialsPath);
                        }
                        else
                        {
                            logger.LogInformation("Loading application default credentials.");
                            appOptions.Credential = GoogleCredential.GetApplicationDefault();
                        }

                        var projectId = configuration["Firebase:ProjectId"];
                        if (!string.IsNullOrWhiteSpace(projectId))
                        {
                            appOptions.ProjectId = projectId;
                        }

                        app = FirebaseApp.Create(appOptions);
                        logger.LogInformation("FirebaseApp created. Name={name}", app.Name);
                    }

                    // Диагностика опций приложения
                    try
                    {
                        var cred = app.Options?.Credential;
                        logger.LogDebug("App options: ProjectId={projectId}, CredentialIsNull={credNull}", app.Options?.ProjectId, cred == null);
                        if (cred != null)
                        {
                            logger.LogDebug("Credential type: {type}", cred.GetType().FullName);
                        }
                    }
                    catch (Exception diagEx)
                    {
                        logger.LogWarning(diagEx, "Failed to read app options for diagnostics");
                    }

                    // Получаем экземпляр FirebaseMessaging для конкретного FirebaseApp
                    try
                    {
                        messagingClient = FirebaseMessaging.GetMessaging(app);
                        if (messagingClient == null)
                        {
                            // Защитный лог — в норме не должен происходить
                            logger.LogError("FirebaseMessaging.GetMessaging returned null");
                            throw new InvalidOperationException("FirebaseMessaging instance is null after initialization.");
                        }
                        logger.LogInformation("FirebaseMessaging initialized successfully (for app {name})", app.Name);
                    }
                    catch (Exception msgEx)
                    {
                        logger.LogError(msgEx, "Failed to obtain FirebaseMessaging instance");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize Firebase");
                throw;
            }
        }

        public async Task SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
                throw new ArgumentException("Device token must be provided", nameof(deviceToken));

            if (messagingClient == null)
                throw new InvalidOperationException("FirebaseMessaging client is not initialized.");

            try
            {
                var message = new Message
                {
                    Token = deviceToken,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data ?? new Dictionary<string, string>()
                };

                var messageId = await messagingClient.SendAsync(message, cancellationToken);
                logger.LogInformation("Notification sent successfully. MessageId: {messageId}", messageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send notification to device token: {deviceToken}", deviceToken);
                throw;
            }
        }

        //public async Task SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
        //{
        //    if (deviceTokens == null) throw new ArgumentNullException(nameof(deviceTokens));
        //    if (messagingClient == null) throw new InvalidOperationException("FirebaseMessaging client is not initialized.");

        //    try
        //    {
        //        if (deviceTokens.Count == 0)
        //        {
        //            logger.LogWarning("No device tokens provided for multicast notification");
        //            return;
        //        }

        //        var multicastMessage = new MulticastMessage
        //        {
        //            Tokens = deviceTokens,
        //            Notification = new Notification
        //            {
        //                Title = title,
        //                Body = body
        //            },
        //            Data = data ?? new Dictionary<string, string>()
        //        };

        //        var response = await messagingClient.SendMulticastAsync(multicastMessage, cancellationToken);
        //        logger.LogInformation(
        //            "Multicast notification sent. Successful: {successful}, Failed: {failed}",
        //            response.SuccessCount,
        //            response.FailureCount);

        //        if (response.FailureCount > 0)
        //        {
        //            logger.LogWarning("Some notifications failed to send. Failed count: {failureCount}", response.FailureCount);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "Failed to send multicast notification");
        //        throw;
        //    }
        //}


        // Замените реализацию SendMulticastAsync на эту (или вставьте в ваш FcmService.cs)

        public async Task SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
        {
            if (deviceTokens == null) throw new ArgumentNullException(nameof(deviceTokens));
            if (messagingClient == null) throw new InvalidOperationException("FirebaseMessaging client is not initialized.");

            if (deviceTokens.Count == 0)
            {
                logger.LogWarning("No device tokens provided for multicast notification");
                return;
            }

            var multicastMessage = new MulticastMessage
            {
                Tokens = deviceTokens,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data ?? new Dictionary<string, string>()
            };

            try
            {
                var response = await messagingClient.SendMulticastAsync(multicastMessage, cancellationToken);
                logger.LogInformation(
                    "Multicast notification sent. Successful: {successful}, Failed: {failed}",
                    response.SuccessCount,
                    response.FailureCount);

                if (response.FailureCount > 0)
                {
                    logger.LogWarning("Some notifications failed to send. Failed count: {failureCount}", response.FailureCount);
                }

                return;
            }
            catch (Exception ex)
            {
                // Логируем подробности
                logger.LogWarning(ex, "Multicast SendMulticastAsync failed, will attempt per-message fallback.");

                // Дополнительная диагностика: попробуем найти в тексте ошибки признаки HTML/404 или /batch
                string text = ex.ToString();
                bool looksLikeBatch404 = text.Contains("/batch", StringComparison.OrdinalIgnoreCase)
                                         || text.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) >= 0
                                         || text.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
                                         || text.Contains("404", StringComparison.OrdinalIgnoreCase);

                if (!looksLikeBatch404)
                {
                    // если это не похоже на /batch 404, логируем и всё равно делаем fallback (устойчивее)
                    logger.LogDebug("Exception did not explicitly indicate /batch 404, but proceeding to fallback send-by-one anyway.");
                }
            }

            // FALLBACK: отправляем по одному (с ограничением параллелизма)
            var successCount = 0;
            var failureCount = 0;
            var concurrency = Math.Min(10, deviceTokens.Count); // ограничение параллелизма
            var semaphore = new System.Threading.SemaphoreSlim(concurrency);
            var tasks = new List<Task>();

            foreach (var token in deviceTokens)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var msg = new Message
                        {
                            Token = token,
                            Notification = new Notification { Title = title, Body = body },
                            Data = data ?? new Dictionary<string, string>()
                        };

                        var id = await messagingClient.SendAsync(msg, cancellationToken).ConfigureAwait(false);
                        Interlocked.Increment(ref successCount);
                        logger.LogDebug("Fallback: message sent to {token}. MessageId: {id}", token, id);
                    }
                    catch (Exception e)
                    {
                        Interlocked.Increment(ref failureCount);
                        logger.LogWarning(e, "Fallback send failed for token {token}", token);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            logger.LogInformation("Fallback multicast done. Successful: {successful}, Failed: {failed}", successCount, failureCount);
            if (failureCount > 0)
            {
                logger.LogWarning("Fallback send completed with failures: {failureCount}", failureCount);
            }
        }
    }
}