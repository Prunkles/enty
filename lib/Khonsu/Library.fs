namespace Khonsu

type IHttpRequestContext =
    abstract GetHeader: key: string -> string
    abstract Path: string
    abstract QueryString: string

type HttpGetRequestDeclaration =
    { Route: string
      RequestBuilder: IHttpRequestContext -> 'Request }

(*

Client:

Request -> HttpRequest
HttpResponse -> Response

Receives: Request -> Response

----

Server:

HttpRequest -> Request
Response -> HttpResponse

Implements: Request -> Response


*)

type HttpGetResponseDeclaration =
    { Body: 'TBody
      Headers: string list }

type HttpGetEndpointDeclaration =
    { Request: HttpGetRequestDeclaration
      Response: HttpGetResponseDeclaration }

//type HttpEndpoint =
//    | Get of HttpGetEndpointDeclaration
