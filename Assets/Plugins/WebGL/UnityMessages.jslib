// Assets/Plugins/UnityMessages.jslib
mergeInto(LibraryManager.library, {
  RegisterMessageListener: function () {
    console.log("[jslib] RegisterMessageListener called");

    if (window.__unityMessageListenerAdded) return;
    window.__unityMessageListenerAdded = true;

    window.addEventListener("message", function (event) {
      const data = event.data || {};
      if (!data.type) return;

      console.log("[jslib] message:", data);

      function sendToUnity(methodName, value) {
        if (typeof SendMessage === "function") {
          SendMessage("TokenManager", methodName, value);
        } else if (window.unityInstance && typeof window.unityInstance.SendMessage === "function") {
          window.unityInstance.SendMessage("TokenManager", methodName, value);
        } else {
          console.warn("[jslib] Unity SendMessage not available for", methodName);
        }
      }

      // token
      if (data.type === "token") {
        const token = data.value || "";
        console.log("[jslib] token:", token);
        sendToUnity("ReceiveTokenFromJS", token);
      }

      // lang
      if (data.type === "lang") {
        const lang = data.value || "";
        console.log("[jslib] lang:", lang);
        sendToUnity("ReceiveLanguageFromJS", lang);
      }

      // fullscreen
      if (data.type === "fullscreen") {
        console.log("[jslib] fullscreen requested");
        if (window.unityInstance && typeof window.unityInstance.SetFullscreen === "function") {
          window.unityInstance.SetFullscreen(1);
        } else {
          console.warn("[jslib] unityInstance.SetFullscreen not available");
        }
      }
    });
  }
});
