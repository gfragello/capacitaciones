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
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://microsoft.com/webservices/", ConfigurationName="ServiceEnviarDatosOVAL.ServiceSoap")]
    public interface ServiceSoap {
        
        // CODEGEN: Generating message contract since message AuthenticationUserRequest has headers
        [System.ServiceModel.OperationContractAttribute(Action="http://microsoft.com/webservices/AuthenticationUser", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        Cursos.ServiceEnviarDatosOVAL.AuthenticationUserResponse AuthenticationUser(Cursos.ServiceEnviarDatosOVAL.AuthenticationUserRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://microsoft.com/webservices/AuthenticationUser", ReplyAction="*")]
        System.Threading.Tasks.Task<Cursos.ServiceEnviarDatosOVAL.AuthenticationUserResponse> AuthenticationUserAsync(Cursos.ServiceEnviarDatosOVAL.AuthenticationUserRequest request);
        
        // CODEGEN: Generating message contract since message get_induccionRequest has headers
        [System.ServiceModel.OperationContractAttribute(Action="http://microsoft.com/webservices/get_induccion", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        Cursos.ServiceEnviarDatosOVAL.get_induccionResponse get_induccion(Cursos.ServiceEnviarDatosOVAL.get_induccionRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://microsoft.com/webservices/get_induccion", ReplyAction="*")]
        System.Threading.Tasks.Task<Cursos.ServiceEnviarDatosOVAL.get_induccionResponse> get_induccionAsync(Cursos.ServiceEnviarDatosOVAL.get_induccionRequest request);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.3056.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://microsoft.com/webservices/")]
    public partial class TokenSucurity : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string usernameField;
        
        private string passwordField;
        
        private string authenTokenField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string Username {
            get {
                return this.usernameField;
            }
            set {
                this.usernameField = value;
                this.RaisePropertyChanged("Username");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string Password {
            get {
                return this.passwordField;
            }
            set {
                this.passwordField = value;
                this.RaisePropertyChanged("Password");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string AuthenToken {
            get {
                return this.authenTokenField;
            }
            set {
                this.authenTokenField = value;
                this.RaisePropertyChanged("AuthenToken");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
                this.RaisePropertyChanged("AnyAttr");
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
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.3056.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://microsoft.com/webservices/")]
    public partial class WebServiceResponse : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string resultField;
        
        private string errorMessageField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string Result {
            get {
                return this.resultField;
            }
            set {
                this.resultField = value;
                this.RaisePropertyChanged("Result");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string ErrorMessage {
            get {
                return this.errorMessageField;
            }
            set {
                this.errorMessageField = value;
                this.RaisePropertyChanged("ErrorMessage");
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
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="AuthenticationUser", WrapperNamespace="http://microsoft.com/webservices/", IsWrapped=true)]
    public partial class AuthenticationUserRequest {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://microsoft.com/webservices/")]
        public Cursos.ServiceEnviarDatosOVAL.TokenSucurity TokenSucurity;
        
        public AuthenticationUserRequest() {
        }
        
        public AuthenticationUserRequest(Cursos.ServiceEnviarDatosOVAL.TokenSucurity TokenSucurity) {
            this.TokenSucurity = TokenSucurity;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="AuthenticationUserResponse", WrapperNamespace="http://microsoft.com/webservices/", IsWrapped=true)]
    public partial class AuthenticationUserResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://microsoft.com/webservices/", Order=0)]
        public string AuthenticationUserResult;
        
        public AuthenticationUserResponse() {
        }
        
        public AuthenticationUserResponse(string AuthenticationUserResult) {
            this.AuthenticationUserResult = AuthenticationUserResult;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="get_induccion", WrapperNamespace="http://microsoft.com/webservices/", IsWrapped=true)]
    public partial class get_induccionRequest {
        
        [System.ServiceModel.MessageHeaderAttribute(Namespace="http://microsoft.com/webservices/")]
        public Cursos.ServiceEnviarDatosOVAL.TokenSucurity TokenSucurity;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://microsoft.com/webservices/", Order=0)]
        public string tipo_doc;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://microsoft.com/webservices/", Order=1)]
        public string rut_trabajador;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://microsoft.com/webservices/", Order=2)]
        public string tipo_inducción;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://microsoft.com/webservices/", Order=3)]
        public string estado_induccion;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://microsoft.com/webservices/", Order=4)]
        public string fecha_inducción;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://microsoft.com/webservices/", Order=5)]
        public string Notas;
        
        public get_induccionRequest() {
        }
        
        public get_induccionRequest(Cursos.ServiceEnviarDatosOVAL.TokenSucurity TokenSucurity, string tipo_doc, string rut_trabajador, string tipo_inducción, string estado_induccion, string fecha_inducción, string Notas) {
            this.TokenSucurity = TokenSucurity;
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
    [System.ServiceModel.MessageContractAttribute(WrapperName="get_induccionResponse", WrapperNamespace="http://microsoft.com/webservices/", IsWrapped=true)]
    public partial class get_induccionResponse {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://microsoft.com/webservices/", Order=0)]
        public Cursos.ServiceEnviarDatosOVAL.WebServiceResponse get_induccionResult;
        
        public get_induccionResponse() {
        }
        
        public get_induccionResponse(Cursos.ServiceEnviarDatosOVAL.WebServiceResponse get_induccionResult) {
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
        Cursos.ServiceEnviarDatosOVAL.AuthenticationUserResponse Cursos.ServiceEnviarDatosOVAL.ServiceSoap.AuthenticationUser(Cursos.ServiceEnviarDatosOVAL.AuthenticationUserRequest request) {
            return base.Channel.AuthenticationUser(request);
        }
        
        public string AuthenticationUser(Cursos.ServiceEnviarDatosOVAL.TokenSucurity TokenSucurity) {
            Cursos.ServiceEnviarDatosOVAL.AuthenticationUserRequest inValue = new Cursos.ServiceEnviarDatosOVAL.AuthenticationUserRequest();
            inValue.TokenSucurity = TokenSucurity;
            Cursos.ServiceEnviarDatosOVAL.AuthenticationUserResponse retVal = ((Cursos.ServiceEnviarDatosOVAL.ServiceSoap)(this)).AuthenticationUser(inValue);
            return retVal.AuthenticationUserResult;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<Cursos.ServiceEnviarDatosOVAL.AuthenticationUserResponse> Cursos.ServiceEnviarDatosOVAL.ServiceSoap.AuthenticationUserAsync(Cursos.ServiceEnviarDatosOVAL.AuthenticationUserRequest request) {
            return base.Channel.AuthenticationUserAsync(request);
        }
        
        public System.Threading.Tasks.Task<Cursos.ServiceEnviarDatosOVAL.AuthenticationUserResponse> AuthenticationUserAsync(Cursos.ServiceEnviarDatosOVAL.TokenSucurity TokenSucurity) {
            Cursos.ServiceEnviarDatosOVAL.AuthenticationUserRequest inValue = new Cursos.ServiceEnviarDatosOVAL.AuthenticationUserRequest();
            inValue.TokenSucurity = TokenSucurity;
            return ((Cursos.ServiceEnviarDatosOVAL.ServiceSoap)(this)).AuthenticationUserAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        Cursos.ServiceEnviarDatosOVAL.get_induccionResponse Cursos.ServiceEnviarDatosOVAL.ServiceSoap.get_induccion(Cursos.ServiceEnviarDatosOVAL.get_induccionRequest request) {
            return base.Channel.get_induccion(request);
        }
        
        public Cursos.ServiceEnviarDatosOVAL.WebServiceResponse get_induccion(Cursos.ServiceEnviarDatosOVAL.TokenSucurity TokenSucurity, string tipo_doc, string rut_trabajador, string tipo_inducción, string estado_induccion, string fecha_inducción, string Notas) {
            Cursos.ServiceEnviarDatosOVAL.get_induccionRequest inValue = new Cursos.ServiceEnviarDatosOVAL.get_induccionRequest();
            inValue.TokenSucurity = TokenSucurity;
            inValue.tipo_doc = tipo_doc;
            inValue.rut_trabajador = rut_trabajador;
            inValue.tipo_inducción = tipo_inducción;
            inValue.estado_induccion = estado_induccion;
            inValue.fecha_inducción = fecha_inducción;
            inValue.Notas = Notas;
            Cursos.ServiceEnviarDatosOVAL.get_induccionResponse retVal = ((Cursos.ServiceEnviarDatosOVAL.ServiceSoap)(this)).get_induccion(inValue);
            return retVal.get_induccionResult;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<Cursos.ServiceEnviarDatosOVAL.get_induccionResponse> Cursos.ServiceEnviarDatosOVAL.ServiceSoap.get_induccionAsync(Cursos.ServiceEnviarDatosOVAL.get_induccionRequest request) {
            return base.Channel.get_induccionAsync(request);
        }
        
        public System.Threading.Tasks.Task<Cursos.ServiceEnviarDatosOVAL.get_induccionResponse> get_induccionAsync(Cursos.ServiceEnviarDatosOVAL.TokenSucurity TokenSucurity, string tipo_doc, string rut_trabajador, string tipo_inducción, string estado_induccion, string fecha_inducción, string Notas) {
            Cursos.ServiceEnviarDatosOVAL.get_induccionRequest inValue = new Cursos.ServiceEnviarDatosOVAL.get_induccionRequest();
            inValue.TokenSucurity = TokenSucurity;
            inValue.tipo_doc = tipo_doc;
            inValue.rut_trabajador = rut_trabajador;
            inValue.tipo_inducción = tipo_inducción;
            inValue.estado_induccion = estado_induccion;
            inValue.fecha_inducción = fecha_inducción;
            inValue.Notas = Notas;
            return ((Cursos.ServiceEnviarDatosOVAL.ServiceSoap)(this)).get_induccionAsync(inValue);
        }
    }
}
