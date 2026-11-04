using System;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdventureGame
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Unity olay geri çağrılarından (Button.onClick, event) async metot başlatmak için.
        /// "async void" içindeki yakalanmamış hatalar uygulamayı kararsız bırakır; burada
        /// her hata yakalanır ve loglanır, oyun asla çökmez.
        /// </summary>
        public static async void Forget(this Task task, Object context = null)
        {
            if (task == null)
            {
                return;
            }

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // İptal beklenen bir durumdur (örn. sahne kapanırken) – sessizce yut.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, context);
            }
        }
    }
}
