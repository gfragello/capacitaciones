﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Cursos.ServiceEnviarDatosOVAL {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="WebServiceResponse", Namespace="http://microsoft.com/webservices/")]
    [System.SerializableAttribute()]
    public partial class WebServiceResponse : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string ResultField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string ErrorMessageField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false)]
        public string Result {
            get {
                return this.ResultField;
            }
            set {
                if ((object.ReferenceEquals(this.ResultField, value) != true)) {
                    this.ResultField = value;
                    this.RaisePropertyChanged("Result");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=1)]
        public string ErrorMessage {
            get {
                return this.ErrorMessageField;
            }
            set {
                if ((object.ReferenceEquals(this.ErrorMessageField, value) != true)) {
                    this.ErrorMessageField = value;
                    this.RaisePropertyChanged("ErrorMessage");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://microsoft.com/webservices/", ConfigurationName="ServiceEnviarDatosOVAL.ServiceSoap")]
    public interface ServiceSoap {
        
        // CODEGEN: Generating message contract since element name tipo_doc from namespace http://microsoft.com/webservices/ is not marked nillable
        [System.ServiceModel.OperationContractAttribute(Action="http://microsoft.com/webservices/get_induccion", ReplyAction="*")]
        Cursos.ServiceEnviarDatosOVAL.get_induccionResponse get_induccion(Cursos.ServiceEnviarDatosOVAL.get_induccionRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://microsoft.com/webservices/get_induccion", ReplyAction="*")]
        System.Threading.Tasks.Task<Cursos.ServiceEnviarDatosOVAL.get_induccionResponse> get_induccionAsync(Cursos.ServiceEnviarDatosOVAL.get_induccionRequest request);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class get_induccionRequest {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Name="get_induccion", Namespace="http://microsoft.com/webservices/", Order=0)]
        public Cursos.ServiceEnviarDatosOVAL.get_induccionRequestBody Body;
        
        public get_induccionRequest() {
        }
        
        public get_induccionRequest(Cursos.ServiceEnviarDatosOVAL.get_induccionRequestBody Body) {
            this.Body = Body;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.Runtime.Serialization.DataContractAttribute(Namespace="http://microsoft.com/webservices/")]
    public partial class get_induccionRequestBody {
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=0)]
        public string tipo_doc;
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=1)]
        public string rut_trabajador;
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=2)]
        public string tipo_inducción;
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=3)]
        public string estado_induccion;
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=4)]
        public string fecha_inducción;
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=5)]
        public string Notas;
        
        public get_induccionRequestBody() {
        }
        
        public get_induccionRequestBody(string tipo_doc, string rut_trabajador, string tipo_inducción, string estado_induccion, string fecha_inducción, string Notas) {
            this.tipo_doc = tipo_doc;
            this.rut_trabajador = rut_trabajador;
            this.tipo_inducción = tipo_inducción;
            this.estado_induccion = estado_induccion;
            this.fecha_inducción = fecha_inducción;
            this.Notas = Notas;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class get_induccionResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Name="get_induccionResponse", Namespace="http://microsoft.com/webservices/", Order=0)]
        public Cursos.ServiceEnviarDatosOVAL.get_induccionResponseBody Body;
        
        public get_induccionResponse() {
        }
        
        public get_induccionResponse(Cursos.ServiceEnviarDatosOVAL.get_induccionResponseBody Body) {
            this.Body = Body;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.Runtime.Serialization.DataContractAttribute(Namespace="http://microsoft.com/webservices/")]
    public partial class get_induccionResponseBody {
        
        [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue=false, Order=0)]
        public Cursos.ServiceEnviarDatosOVAL.WebServiceResponse get_induccionResult;
        
        public get_induccionResponseBody() {
        }
        
        public get_induccionResponseBody(Cursos.ServiceEnviarDatosOVAL.WebServiceResponse get_induccionResult) {
            this.get_induccionResult = get_induccionResult;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ServiceSoapChannel : Cursos.ServiceEnviarDatosOVAL.ServiceSoap, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class ServiceSoapClient : System.ServiceModel.ClientBase<Cursos.ServiceEnviarDatosOVAL.ServiceSoap>, Cursos.ServiceEnviarDatosOVAL.ServiceSoap {
        
        public ServiceSoapClient() {
        }
        
        public ServiceSoapClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public ServiceSoapClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ServiceSoapClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ServiceSoapClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Cursos.ServiceEnviarDatosOVAL.get_induccionResponse Cursos.ServiceEnviarDatosOVAL.ServiceSoap.get_induccion(Cursos.ServiceEnviarDatosOVAL.get_induccionRequest request) {
            return base.Channel.get_induccion(request);
        }
        
        public Cursos.ServiceEnviarDatosOVAL.WebServiceResponse get_induccion(string tipo_doc, string rut_trabajador, string tipo_inducción, string estado_induccion, string fecha_inducción, string Notas) {
            Cursos.ServiceEnviarDatosOVAL.get_induccionRequest inValue = new Cursos.ServiceEnviarDatosOVAL.get_induccionRequest();
            inValue.Body = new Cursos.ServiceEnviarDatosOVAL.get_induccionRequestBody();
            inValue.Body.tipo_doc = tipo_doc;
            inValue.Body.rut_trabajador = rut_trabajador;
            inValue.Body.tipo_inducción = tipo_inducción;
            inValue.Body.estado_induccion = estado_induccion;
            inValue.Body.fecha_inducción = fecha_inducción;
            inValue.Body.Notas = Notas;
            Cursos.ServiceEnviarDatosOVAL.get_induccionResponse retVal = ((Cursos.ServiceEnviarDatosOVAL.ServiceSoap)(this)).get_induccion(inValue);
            return retVal.Body.get_induccionResult;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<Cursos.ServiceEnviarDatosOVAL.get_induccionResponse> Cursos.ServiceEnviarDatosOVAL.ServiceSoap.get_induccionAsync(Cursos.ServiceEnviarDatosOVAL.get_induccionRequest request) {
            return base.Channel.get_induccionAsync(request);
        }
        
        public System.Threading.Tasks.Task<Cursos.ServiceEnviarDatosOVAL.get_induccionResponse> get_induccionAsync(string tipo_doc, string rut_trabajador, string tipo_inducción, string estado_induccion, string fecha_inducción, string Notas) {
            Cursos.ServiceEnviarDatosOVAL.get_induccionRequest inValue = new Cursos.ServiceEnviarDatosOVAL.get_induccionRequest();
            inValue.Body = new Cursos.ServiceEnviarDatosOVAL.get_induccionRequestBody();
            inValue.Body.tipo_doc = tipo_doc;
            inValue.Body.rut_trabajador = rut_trabajador;
            inValue.Body.tipo_inducción = tipo_inducción;
            inValue.Body.estado_induccion = estado_induccion;
            inValue.Body.fecha_inducción = fecha_inducción;
            inValue.Body.Notas = Notas;
            return ((Cursos.ServiceEnviarDatosOVAL.ServiceSoap)(this)).get_induccionAsync(inValue);
        }
    }
}
