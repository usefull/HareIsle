//------------------------------------------------------------------------------
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
        ///   Ищет локализованную строку, похожую на Invalid payload object: .
        /// </summary>
        public static string InvalidPayload {
            get {
                return ResourceManager.GetString("InvalidPayload", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Invalid queue name. Name can not be empty or blank string, must contains only US-ASCII chars, must not start with &apos;amq.&apos; and be up to 255 chars.
        /// </summary>
        public static string InvalidQueueName {
            get {
                return ResourceManager.GetString("InvalidQueueName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Invalid RPC request.
        /// </summary>
        public static string InvalidRpcRequest {
            get {
                return ResourceManager.GetString("InvalidRpcRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Invalid RPC response.
        /// </summary>
        public static string InvalidRpcResponse {
            get {
                return ResourceManager.GetString("InvalidRpcResponse", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на CorrelationId property is not specified.
        /// </summary>
        public static string NoCorrelationId {
            get {
                return ResourceManager.GetString("NoCorrelationId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на DeliveryTag property is not specified.
        /// </summary>
        public static string NoDeliveryTag {
            get {
                return ResourceManager.GetString("NoDeliveryTag", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на ReplyTo property is not specified.
        /// </summary>
        public static string NoReplyTo {
            get {
                return ResourceManager.GetString("NoReplyTo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на RPC request must not be null.
        /// </summary>
        public static string NullRpcRequest {
            get {
                return ResourceManager.GetString("NullRpcRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на RPC response must not be null.
        /// </summary>
        public static string NullRpcResponse {
            get {
                return ResourceManager.GetString("NullRpcResponse", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Undeserialized RPC response is null.
        /// </summary>
        public static string NullUndeserializedRpcResponse {
            get {
                return ResourceManager.GetString("NullUndeserializedRpcResponse", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на The payload and the error cannot be set at the same time.
        /// </summary>
        public static string PayloadAndErrorCantAtTheSameTime {
            get {
                return ResourceManager.GetString("PayloadAndErrorCantAtTheSameTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Request handler function timeout must be grater than zero.
        /// </summary>
        public static string RequestHandlerFunctionTimeoutMustBeGraterThanZero {
            get {
                return ResourceManager.GetString("RequestHandlerFunctionTimeoutMustBeGraterThanZero", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на RPC protocol error.
        /// </summary>
        public static string RpcProtocolError {
            get {
                return ResourceManager.GetString("RpcProtocolError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на RPC request deserialization error.
        /// </summary>
        public static string RpcRequestDeserializationError {
            get {
                return ResourceManager.GetString("RpcRequestDeserializationError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на RPC request serialization error.
        /// </summary>
        public static string RpcRequestSerializationError {
            get {
                return ResourceManager.GetString("RpcRequestSerializationError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на RPC response decoding error.
        /// </summary>
        public static string RpcResponseDecodingError {
            get {
                return ResourceManager.GetString("RpcResponseDecodingError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на RPC response deserialization error.
        /// </summary>
        public static string RpcResponseDeserializationError {
            get {
                return ResourceManager.GetString("RpcResponseDeserializationError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unexpected error.
        /// </summary>
        public static string UnexpectedError {
            get {
                return ResourceManager.GetString("UnexpectedError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на Unknown RPC error.
        /// </summary>
        public static string UnknownRpcError {
            get {
                return ResourceManager.GetString("UnknownRpcError", resourceCulture);
            }
        }
    }
}
