using System;
using GameCreator.Runtime.Common;
using Unity.Cinemachine;
using UnityEngine;

namespace GameCreator.Runtime.Cinemachine
{
    [Title("Cinemachine Camera")]
    [Category("Cinemachine/Cinemachine Camera")]
    
    [Image(typeof(IconCinemachine), ColorTheme.Type.Blue)]
    [Description("A Cinemachine Camera component reference")]

    [Serializable] [HideLabelsInEditor]
    public class GetGameObjectCinemachine : PropertyTypeGetGameObject
    {
        [SerializeField] protected CinemachineCamera m_CinemachineCamera;

        public override GameObject Get(Args args) => this.m_CinemachineCamera != null 
            ? this.m_CinemachineCamera.gameObject 
            : null;
        
        public override GameObject Get(GameObject gameObject) => this.m_CinemachineCamera != null 
            ? this.m_CinemachineCamera.gameObject 
            : null;

        public override T Get<T>(Args args)
        {
            if (typeof(T) == typeof(CinemachineCamera)) return this.m_CinemachineCamera as T;
            return base.Get<T>(args);
        }

        public GetGameObjectCinemachine() : base()
        { }
        
        public GetGameObjectCinemachine(GameObject gameObject) : this()
        {
            this.m_CinemachineCamera = gameObject.Get<CinemachineCamera>();
        }
        
        public GetGameObjectCinemachine(CinemachineCamera cinemachineCamera) : this()
        {
            this.m_CinemachineCamera = cinemachineCamera;
        }

        public static PropertyGetGameObject Create()
        {
            GetGameObjectCinemachine instance = new GetGameObjectCinemachine();
            return new PropertyGetGameObject(instance);
        }
        
        public static PropertyGetGameObject Create(GameObject gameObject)
        {
            GetGameObjectCinemachine instance = new GetGameObjectCinemachine
            {
                m_CinemachineCamera = gameObject != null ? gameObject.Get<CinemachineCamera>() : null
            };
            
            return new PropertyGetGameObject(instance);
        }
        
        public static PropertyGetGameObject Create(CinemachineCamera CinemachineCamera)
        {
            GetGameObjectCinemachine instance = new GetGameObjectCinemachine
            {
                m_CinemachineCamera = CinemachineCamera
            };
            
            return new PropertyGetGameObject(instance);
        }

        public override string String => this.m_CinemachineCamera != null
            ? this.m_CinemachineCamera.gameObject.name
            : "(none)";

        public override GameObject EditorValue => this.m_CinemachineCamera != null 
            ? this.m_CinemachineCamera.gameObject
            : null;
    }
}