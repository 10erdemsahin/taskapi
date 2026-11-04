using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace AdventureGame
{
    /// <summary>
    /// UnityWebRequest'i async/await ile kullanılabilir hâle getirir.
    /// İstek Unity'nin yerel ağ katmanında arka planda yürür; await sonrası devam
    /// kodu UnitySynchronizationContext sayesinde ANA THREAD'e döner. Böylece
    /// ana döngü (frame rate / UI) hiçbir zaman bloklanmaz ve Unity API'leri güvenle kullanılır.
    /// </summary>
    public static class UnityWebRequestExtensions
    {
        public static Task SendAsync(this UnityWebRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var completionSource = new TaskCompletionSource<bool>();
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            CancellationTokenRegistration registration = default;

            if (cancellationToken.CanBeCanceled)
            {
                // useSynchronizationContext: true → geri çağrı ana thread'de çalışır
                // (CancelAfter zamanlayıcısı başka thread'den tetiklense bile Abort güvenli olur).
                registration = cancellationToken.Register(() =>
                {
                    if (completionSource.Task.IsCompleted)
                    {
                        return;
                    }

                    try
                    {
                        if (!request.isDone)
                        {
                            request.Abort();
                        }
                    }
                    catch (Exception)
                    {
                        // İstek bu arada dispose edilmiş olabilir; görmezden gel.
                    }

                    completionSource.TrySetCanceled(cancellationToken);
                }, useSynchronizationContext: true);
            }

            if (operation.isDone)
            {
                registration.Dispose();
                completionSource.TrySetResult(true);
            }
            else
            {
                operation.completed += _ =>
                {
                    registration.Dispose();
                    completionSource.TrySetResult(true);
                };
            }

            return completionSource.Task;
        }
    }
}
