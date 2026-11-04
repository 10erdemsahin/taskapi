using UnityEngine;

namespace AdventureGame
{
    /// <summary>
    /// Tüm etkileşimli nesneler için soyut taban sınıf (Template Method deseni).
    /// Yetki/uygunluk kontrolü burada bir kez yazılır; alt sınıflar yalnızca
    /// OnInteract içinde kendi davranışlarını tanımlar.
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [Header("Etkileşim")]
        [SerializeField] protected string interactionPrompt = "Etkileşim [E]";

        [Tooltip("Boş bırakılırsa nesnenin kendi Transform'u kullanılır. Yoldaş buraya yürür.")]
        [SerializeField] protected Transform interactionAnchor;

        [SerializeField] protected bool allowPlayer = true;
        [SerializeField] protected bool allowAICompanion = true;

        public virtual string InteractionPrompt => interactionPrompt;

        public Transform InteractionAnchor => interactionAnchor != null ? interactionAnchor : transform;

        /// <summary>Alt sınıflar durumlarına göre (örn. şalter zaten çekildi) kullanılabilirliği kapatabilir.</summary>
        protected virtual bool IsAvailable => true;

        public bool CanInteract(InteractorType interactorType)
        {
            if (!isActiveAndEnabled || !IsAvailable)
            {
                return false;
            }

            return interactorType == InteractorType.Player ? allowPlayer : allowAICompanion;
        }

        public void Interact(GameObject interactor, InteractorType interactorType)
        {
            if (!CanInteract(interactorType))
            {
                return;
            }

            OnInteract(interactor, interactorType);
        }

        protected abstract void OnInteract(GameObject interactor, InteractorType interactorType);
    }
}
