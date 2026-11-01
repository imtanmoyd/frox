import { createRoot } from "react-dom/client";
import { useEffect, useState } from "react";
import { Widget, WidgetErrorBoundary } from "@frox/ui";
import widgetStyles from "../../../packages/ui/src/widget.css?inline";
import shellStyles from "./widget-shell.css?inline";
import { WebSettings } from "./WebSettings";
import { webPlatform } from "./platform";
import "./landing.css";

function ChatShell() {
  const [settingsOpen, setSettingsOpen] = useState(false);

  useEffect(() => {
    const openSettings = () => setSettingsOpen(true);
    window.addEventListener("frox:web-settings", openSettings);
    return () => window.removeEventListener("frox:web-settings", openSettings);
  }, []);

  return (
    <>
      <Widget
        platform={webPlatform}
        siteHost={window.location.hostname}
        enableVoice={false}
        offlineMessage="I’m here. Add a provider in Settings to start a private chat from this browser."
      />
      {settingsOpen && <WebSettings platform={webPlatform} onClose={() => setSettingsOpen(false)} />}
    </>
  );
}

const host = document.createElement("div");
host.id = "frox-web-widget-host";
document.documentElement.appendChild(host);
const shadow = host.attachShadow({ mode: "open" });
const style = document.createElement("style");
style.textContent = `${widgetStyles}\n${shellStyles}`;
shadow.appendChild(style);
const mount = document.createElement("div");
shadow.appendChild(mount);

createRoot(mount).render(
  <WidgetErrorBoundary onError={(error) => {
    void webPlatform.messaging.send({
      type: "assistant:error-log",
      error: { message: error.message, stack: error.stack }
    });
  }}>
    <ChatShell />
  </WidgetErrorBoundary>
);

document.addEventListener("click", (event) => {
  const target = event.target;
  if (!(target instanceof Element)) return;
  const link = target.closest<HTMLAnchorElement>("[data-open-chat]");
  if (!link) return;
  event.preventDefault();
  window.dispatchEvent(new Event("frox-widget-open"));
});
