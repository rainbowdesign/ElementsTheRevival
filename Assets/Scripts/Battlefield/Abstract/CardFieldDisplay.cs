﻿using Battlefield.Abilities;
using Core.Helpers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlefield.Abstract
{
    public class CardFieldDisplay : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected GameObject validTargetGlow;
        [SerializeField] private GameObject isUsableGlow;
        [SerializeField] private FieldObjectAnimation fieldObjectAnimation;
        public Card Card { get; private set; }
        public ID Id { get; private set; }

        public void SetupId(ID newId)
        {
            Id = newId;
            fieldObjectAnimation.SetupId(newId);
            RegisterEvents();
            PlaySoundEffect();
        }

        private void PlaySoundEffect()
        {
            if (Id.IsFromHand())
            {
            }
            else
            {
                EventBus<PlaySoundEffectEvent>.Raise(new PlaySoundEffectEvent("CardPlay"));
            }

        }
        private void RegisterEvents()
        {
            if (!Id.IsFromHand())
            {
                _shouldShowTargetableBinding = new EventBinding<ShouldShowTargetableEvent>(ShouldShowTarget);
                EventBus<ShouldShowTargetableEvent>.Register(_shouldShowTargetableBinding);
                _activateAbilityEffectBinding = new EventBinding<ActivateAbilityEffectEvent>(ActivateAbilityEffect);
                EventBus<ActivateAbilityEffectEvent>.Register(_activateAbilityEffectBinding);
            }

            if (Id.IsOwnedBy(OwnerEnum.Opponent)) return;
            _shouldShowUsableBinding = new EventBinding<ShouldShowUsableEvent>(ShouldShowUsableGlow);
            EventBus<ShouldShowUsableEvent>.Register(_shouldShowUsableBinding);
            _hideUsableDisplayBinding = new EventBinding<HideUsableDisplayEvent>(HideUsableGlow);
            EventBus<HideUsableDisplayEvent>.Register(_hideUsableDisplayBinding);
        }
        protected void SetCard(Card card) => Card = card;
    
        private EventBinding<ShouldShowTargetableEvent> _shouldShowTargetableBinding;
        private EventBinding<ShouldShowUsableEvent> _shouldShowUsableBinding;
        private EventBinding<HideUsableDisplayEvent> _hideUsableDisplayBinding;
        private EventBinding<ActivateAbilityEffectEvent> _activateAbilityEffectBinding;

        private void OnDisable()
        {
            EventBus<ShouldShowTargetableEvent>.Unregister(_shouldShowTargetableBinding);
            EventBus<ShouldShowUsableEvent>.Unregister(_shouldShowUsableBinding);
            EventBus<HideUsableDisplayEvent>.Unregister(_hideUsableDisplayBinding);
            EventBus<ActivateAbilityEffectEvent>.Unregister(_activateAbilityEffectBinding);
        }

        private void Awake()
        {
            isUsableGlow.SetActive(false);
            validTargetGlow.SetActive(false);
        }

        private void ShouldShowTarget(ShouldShowTargetableEvent shouldShowTargetableEvent)
        {
            if (this == null) return;
            if (Id.field.Equals(FieldEnum.Hand)) return;
            validTargetGlow.SetActive(false);
            if (shouldShowTargetableEvent.IsCardValidTarget is null) return;
            var isValid = shouldShowTargetableEvent.IsCardValidTarget(Id, Card);
            if (!isValid) return;
            if (!shouldShowTargetableEvent.ShouldHideGraphic)
            {
                validTargetGlow.SetActive(true);
            }
            EventBus<AddTargetToListEvent>.Raise(new AddTargetToListEvent(Id, Card));
        }

        private void ShouldShowUsableGlow(ShouldShowUsableEvent shouldShowUsableEvent)
        {
            if (this == null) return;
            if (isUsableGlow == null) return;

            if (!shouldShowUsableEvent.Owner.Equals(Id.owner))
            {
                isUsableGlow.SetActive(false);
                return;
            }

            switch (Id.field)
            {
                case FieldEnum.Hand:
                    isUsableGlow.SetActive(shouldShowUsableEvent.QuantaCheck(Card.CostElement, Card.Cost));
                    return;
                default:
                    isUsableGlow.SetActive(Card.IsAbilityUsable(shouldShowUsableEvent.QuantaCheck, shouldShowUsableEvent.HandCount));
                    return;
            }
        }
        
        private void HideUsableGlow(HideUsableDisplayEvent hideUsableDisplayEvent)
        {
            if (this == null) return;
            isUsableGlow.SetActive(false);
        }
        
        private void ActivateAbilityEffect(ActivateAbilityEffectEvent activateAbilityEffectEvent)
        {
            if (this == null) return;
            if (!activateAbilityEffectEvent.TargetId.Equals(Id)) return;
            activateAbilityEffectEvent.ActivateAbilityEffect(Id, Card);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if ((Id, Card).HasCard())
            {
                EventBus<CardTappedEvent>.Raise(new CardTappedEvent(Id, Card));
            }
            EventBus<DisplayCardToolTipEvent>.Raise(new DisplayCardToolTipEvent(Id, Card, true));
        }

        internal void ClearCardDisplay(bool isClear)
        {
            var ability = Card.PlayRemoveAbility;
            if (isClear)
            {
                EventBus<PlayAnimationEvent>.Raise(new PlayAnimationEvent(Id, "CardDeath", Element.Air));
                EventBus<RemoveCardFromManagerEvent>.Raise(new RemoveCardFromManagerEvent(Id));
            }
            CheckOnRemoveEffects(ability);
            if (isClear)
            {
                Destroy(gameObject);
            }
        }

        private void CheckOnRemoveEffects(OnPlayRemoveAbility removeAbility)
        {
            removeAbility?.OnRemoveActivate(Id, Card);
            if (removeAbility is CloakPlayRemoveAbility)
            {
                EventBus<UpdateCloakParentEvent>.Raise(new UpdateCloakParentEvent(transform, Id, false));
            }
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Id.field.Equals(FieldEnum.Hand) && Id.IsOwnedBy(OwnerEnum.Opponent)) return;
            if (Card.Id is "4t1" or "4t2") return;
            var rectTransform = GetComponent<RectTransform>();
            Vector2 objectSize = new(rectTransform.rect.height, rectTransform.rect.width);
            EventBus<DisplayCardToolTipEvent>.Raise(new DisplayCardToolTipEvent(Id, Card, false));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EventBus<DisplayCardToolTipEvent>.Raise(new DisplayCardToolTipEvent(Id, Card, true));
        }
    }
}