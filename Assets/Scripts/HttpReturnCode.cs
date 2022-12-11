using System;
using System.Linq;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class HttpReturnCode {
    public int code { get; private set; }
    public string message { get; private set; }
    public QueryResponse queryResponse { get; private set; }


    public HttpReturnCode() {
        this.code = 200;
        this.message = "Success";
        this.queryResponse = null;
    }

    public HttpReturnCode(String message, QueryResponse queryResponse) {
        this.code = 200;
        this.message = message;
        this.queryResponse = queryResponse;
    }
    public HttpReturnCode(int code, string message) {
        this.code = code;
        this.message = message;
        this.queryResponse = null;
    }

    public HttpReturnCode(Exception e) {
        this.code = 400;
        this.message = e.ToString();
        this.queryResponse = null;
    }

    public override string ToString() {
        return "HTTP" + code.ToString() + " : " + message;
    }

    public void Log() {
        if (Enumerable.Range(200, 299).Contains(code)) {
            Debug.Log(ToString());
        } else if (Enumerable.Range(500, 599).Contains(code)) {
            Debug.LogWarning(ToString());
        } else {
            Debug.LogError(ToString());
        }
    }

    public bool IsSuccess() {
        return (Enumerable.Range(200, 299).Contains(code)) ? true : false;
    }
}
