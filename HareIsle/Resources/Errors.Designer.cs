﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HareIsle.Resources {
    using System;
    
    
    /// <summary>
    ///   Класс ресурса со строгой типизацией для поиска локализованных строк и т.д.
    /// </summary>
    // Этот класс создан автоматически классом StronglyTypedResourceBuilder
    // с помощью такого средства, как ResGen или Visual Studio.
    // Чтобы добавить или удалить член, измените файл .ResX и снова запустите ResGen
    // с параметром /str или перестройте свой проект VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Errors {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Errors() {
        }
        
        /// <summary>
        ///   Возвращает кэшированный экземпляр ResourceManager, использованный этим классом.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("HareIsle.Resources.Errors", typeof(Errors).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Перезаписывает свойство CurrentUICulture текущего потока для всех
        ///   обращений к ресурсу с помощью этого класса ресурса со строгой типизацией.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на An actor ID cannot be null, empty or blank string..
        /// </summary>
        public static string ActorIdCannotBeNullEmptyOrBlank {
            get {
                return ResourceManager.GetString("ActorIdCannotBeNullEmptyOrBlank", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Couldn&apos;t wait for ack..
        /// </summary>
        public static string CouldNotWaitForAck {
            get {
                return ResourceManager.GetString("CouldNotWaitForAck", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Attempt to create a handler on a closed RabbitMQ connection..
        /// </summary>
        public static string HandlerCreationOnClosedConnection {
            get {
                return ResourceManager.GetString("HandlerCreationOnClosedConnection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на A queue name cannot starts with prefixes: {0}..
        /// </summary>
        public static string InvalidQueueName {
            get {
                return ResourceManager.GetString("InvalidQueueName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Message nack error. Message rejected..
        /// </summary>
        public static string MessageNackError {
            get {
                return ResourceManager.GetString("MessageNackError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Message routing error. The most likely reason is the lack of a receiving queue..
        /// </summary>
        public static string MessageRoutingError {
            get {
                return ResourceManager.GetString("MessageRoutingError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Payload object is null..
        /// </summary>
        public static string NullPayload {
            get {
                return ResourceManager.GetString("NullPayload", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на The payload type in the message does not match the declared one..
        /// </summary>
        public static string PayloadTypeMismatch {
            get {
                return ResourceManager.GetString("PayloadTypeMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на A queue name cannot be null, empty or blank..
        /// </summary>
        public static string QueueNameCannotBeNullEmptyOrBlank {
            get {
                return ResourceManager.GetString("QueueNameCannotBeNullEmptyOrBlank", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Throws in case of a message sending error..
        /// </summary>
        public static string SendingError {
            get {
                return ResourceManager.GetString("SendingError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Serializing error..
        /// </summary>
        public static string SerializingError {
            get {
                return ResourceManager.GetString("SerializingError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unknown validation error..
        /// </summary>
        public static string UnknownValidationError {
            get {
                return ResourceManager.GetString("UnknownValidationError", resourceCulture);
            }
        }
    }
}
