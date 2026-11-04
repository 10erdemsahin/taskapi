using UnityEngine;
using Object = UnityEngine.Object;

namespace AdventureGame
{
    /// <summary>
    /// Unity Inspector arayüz (interface) tipindeki alanları serileştiremez.
    /// Bu yardımcı, Inspector'a sürüklenen MonoBehaviour/GameObject içinden
    /// istenen arayüzü güvenli biçimde bulur; bulamazsa açıklayıcı hata loglar
    /// ve null döner (NullReferenceException yerine kontrollü hata).
    /// </summary>
    public static class InterfaceResolver
    {
        public static T Resolve<T>(Object source, Object owner, string fieldName) where T : class
        {
            string ownerName = owner != null ? owner.name : "?";

            if (source == null)
            {
                Debug.LogError($"[{ownerName}] '{fieldName}' alanı atanmamış ({typeof(T).Name} bekleniyor).", owner);
                return null;
            }

            if (source is T direct)
            {
                return direct;
            }

            // Bir GameObject sürüklendiğinde Unity ilk MonoBehaviour'u atar; doğru bileşeni ara.
            if (source is Component component && component.TryGetComponent(out T fromComponent))
            {
                return fromComponent;
            }

            if (source is GameObject gameObject && gameObject.TryGetComponent(out T fromGameObject))
            {
                return fromGameObject;
            }

            Debug.LogError($"[{ownerName}] '{fieldName}' alanına atanan '{source.name}' {typeof(T).Name} uygulamıyor.", owner);
            return null;
        }
    }
}
