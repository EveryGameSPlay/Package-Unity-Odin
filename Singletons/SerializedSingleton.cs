﻿using System;
using Egsp.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Egsp.Core
{
    public class SerializedSingleton<TSingleton> : SerializedMonoBehaviour where TSingleton : SerializedMonoBehaviour
    {
        public static TSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    if(!AllowLazyInstance)
                        throw new LazyInstanceException(typeof(TSingleton));
                    
                    _instance = FindObjectOfType<TSingleton>();

                    if (_instance == null)
                    {
                        CreateInstance();
                    }
                    else
                    {
                        if (_instanceGameObject == null)
                            _instanceGameObject = _instance.gameObject;
                    }
                }

                return _instance;
            }
            protected set => _instance = value;
        }
        
        /// <summary>
        /// Разрешена ли инициализация при обращении к экземпляру.
        /// </summary>
        protected static bool AllowLazyInstance
        {
            get
            {
                var lazyAttribute = (LazyInstanceAttribute)Attribute.GetCustomAttribute(typeof(TSingleton), 
                        typeof(LazyInstanceAttribute));

                if (lazyAttribute == null)
                    return true;

                return lazyAttribute.AllowLazyInstance;
            }
        }
        
        /// <summary>
        /// Помечает экземпляр как DontDestroyOnLoad.
        /// Изначальное значение false.
        /// </summary>
        public static bool AlwaysExist
        {
            get => alwaysExist;
            set
            {
                if(value == true)
                    DontDestroyOnLoad(_instance);
            }
        }
        private static bool alwaysExist;

        private static TSingleton _instance;
        private static GameObject _instanceGameObject;

        public static WeakEvent<TSingleton> OnInstanceCreated = new WeakEvent<TSingleton>();


        /// <param name="immediate">Уничтожить мгновенно, а не в конце кадра.</param>
        public static void DestroyIfExist(bool immediate = false)
        {
            if (_instance != null)
            {
                // Данная операция срабатывает в OnDestroy.
                // _instance = null;
                
                if (_instanceGameObject == null)
                    return;
                
                if (immediate)
                {
                    DestroyImmediate(_instanceGameObject);
                }
                else
                {
                    Destroy(_instanceGameObject);
                }
            }
        }

        /// <summary>
        /// Нужно использовать если объект имеет атрибут LazyInstance(false).
        /// Можно использовать для ручного создания.
        /// </summary>
        public static TSingleton CreateInstance()
        {
            if (_instance != null)
            {
                if (_instanceGameObject != null)
                    DestroyImmediate(_instanceGameObject);

                _instance = null;
            }
            
            CreateInstanceSafely();

            return _instance;
        }

        /// <summary>
        /// При добавлении компонента у него пройдет вызов метода Awake и значения будут записаны компонентом.
        /// И хотя вызов пройдет, значения будут перезаписаны нами для надежности.
        /// Дочерние классы могут сокрыть определение метода Awake.
        /// </summary>
        private static void CreateInstanceSafely()
        {
            var singletonGameObject = new GameObject("[Singleton]" + typeof(TSingleton));
            _instance = singletonGameObject.AddComponent<TSingleton>();
            _instanceGameObject = singletonGameObject;
        }

        /// <summary>
        /// В этом случае значения экземпляра и объекта НЕ будут записаны вручную! 
        /// Это нужно в том случае, если вы НЕ допускаете возможность сокрытия метода Awake в дочерних классах.
        /// НЕ РЕКОМЕНДОВАНО!
        /// </summary>
        private static void CreateInstanceUnsafely()
        {
            var singletonGameObject = new GameObject("[Singleton]" + typeof(TSingleton),
                typeof(TSingleton));
        }

        protected virtual void Awake()
        {
            // Если на сцене уже существует экземпляр, то текущий нужно уничтожить.
            if (_instance != null)
            {
                DestroyImmediate(gameObject);
            }
            // Этот блок кода обрабатывает ситуацию, когда объект не создается из вне, а существует при старте сцены.
            // Также он сработает при вызове CreateInstance и сам назначит значения.
            else
            {
                _instance = this as TSingleton;
                _instanceGameObject = gameObject;
                OnInstanceCreatedInternal();
            }
        }
        
        /// <summary>
        /// Вызывается при создании экземпляра.
        /// </summary>
        protected virtual void OnInstanceCreatedInternal()
        {
            OnInstanceCreated.Raise(_instance);
        }

        protected virtual void OnDestroy()
        {
            _instance = null;
            _instanceGameObject = null;
            // Нужно очистить значение, т.к. новые подписчики будут получать ссылку на старый инстанс.
            OnInstanceCreated.ClearCachedValue();
        }
    }
}