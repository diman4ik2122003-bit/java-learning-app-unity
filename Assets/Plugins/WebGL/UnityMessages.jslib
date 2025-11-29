mergeInto(LibraryManager.library, {
  RegisterMessageListener: function () {
    console.log("[jslib] RegisterMessageListener called");
    window.addEventListener("message", function (event) {
      const data = event.data || {};
      console.log("[jslib] message", data);
      if (!data.type) return;

      // token
      if (data.type === "token") {
        const token = data.value || "";
        console.log("[jslib] token", token);
        if (typeof SendMessage === "function") {
          SendMessage("TokenManager", "ReceiveTokenFromJS", token);
        } else if (window.unityInstance) {
          window.unityInstance.SendMessage("TokenManager", "ReceiveTokenFromJS", token);
        }
      }

      // lang
      if (data.type === "lang") {
        const lang = data.value || "";
        console.log("[jslib] lang", lang);
        if (typeof SendMessage === "function") {
          SendMessage("TokenManager", "ReceiveLanguageFromJS", lang);
        } else if (window.unityInstance) {
          window.unityInstance.SendMessage("TokenManager", "ReceiveLanguageFromJS", lang);
        }
      }
    });
  }
});
