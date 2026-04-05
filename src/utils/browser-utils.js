export async function copyTextToClipboard(text) {
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(text);
    return;
  }

  const textarea = document.createElement("textarea");
  textarea.value = text;
  textarea.setAttribute("readonly", "true");
  textarea.style.position = "fixed";
  textarea.style.opacity = "0";
  document.body.appendChild(textarea);
  textarea.focus();
  textarea.select();

  const success = document.execCommand("copy");
  document.body.removeChild(textarea);

  if (!success) {
    throw new Error("当前环境不支持剪贴板写入。");
  }
}

export function downloadBlobFile(filename, content, mimeType) {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");

  anchor.href = url;
  anchor.download = filename;
  anchor.click();

  URL.revokeObjectURL(url);
}

export function pickFileFromBrowser({ accept = "*/*" } = {}) {
  return new Promise((resolve, reject) => {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = accept;
    input.style.position = "fixed";
    input.style.opacity = "0";
    input.style.pointerEvents = "none";

    input.onchange = () => {
      const file = input.files?.[0] || null;
      input.remove();

      if (!file) {
        reject(new Error("未选择文件。"));
        return;
      }

      resolve(file);
    };

    input.oncancel = () => {
      input.remove();
      reject(new Error("已取消选择文件。"));
    };

    document.body.appendChild(input);
    input.click();
  });
}
