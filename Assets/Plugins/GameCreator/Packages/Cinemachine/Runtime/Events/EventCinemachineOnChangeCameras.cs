using System;
using GameCreator.Runtime.Common;
using GameCreator.Runtime.VisualScripting;
using Unity.Cinemachine;
using UnityEngine;

namespace GameCreator.Runtime.Cinemachine
{
    [Title("On Cinemachine Change Cameras")]
    [Category("Cinemachine/On Cinemachine Change Cameras")]
    [Description("Executed when the outgoing and/or incoming Cinemachine Camera are activated and start transitioning")]

    [Image(typeof(IconCinemachine), ColorTheme.Type.Blue)]
    [Keywords("Camera", "Blend", "Change")]
    
    [Serializable]
    public class EventCinemachineOnChangeCameras : VisualScripting.Event
    {
        // MEMBERS: -------------------------------------------------------------------------------

        [SerializeField] private CompareGameObjectOrAny m_OutgoingCamera = new CompareGameObjectOrAny(
            true,
            GetGameObjectCinemachine.Create()
        );
        
        [SerializeField] private CompareGameObjectOrAny m_IncomingCamera = new CompareGameObjectOrAny(
            true,
            GetGameObjectCinemachine.Create()
        );

        // INITIALIZERS: --------------------------------------------------------------------------
        
        protected override void OnEnable(Trigger trigger)
        {
            base.OnEnable(trigger);
            CinemachineCore.CameraActivatedEvent.AddListener(this.OnCinemachineEvent);
        }
        
        protected override void OnDisable(Trigger trigger)
        {
            base.OnDisable(trigger);
            if (ApplicationManager.IsExiting) return;
            CinemachineCore.CameraActivatedEvent.RemoveListener(this.OnCinemachineEvent);
        }
        
        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void OnCinemachineEvent(ICinemachineCamera.ActivationEventParams activation)
        {
            if (ApplicationManager.IsExiting) return;
            
            GameObject incoming = activation.IncomingCamera is Component component1 ? component1.gameObject : null;
            GameObject outgoing = activation.OutgoingCamera is Component component2 ? component2.gameObject : null;

            if (this.m_OutgoingCamera.Match(outgoing, this.m_Trigger.gameObject) &&
                this.m_IncomingCamera.Match(incoming, this.m_Trigger.gameObject))
            {
                _ = this.m_Trigger.Execute(this.Self);
            }
        }
    }
}