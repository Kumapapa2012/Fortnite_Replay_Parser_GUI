// --- DOM 要素 ---
const replayFileInput = document.getElementById("replayFileInput");
const replayFilePath = document.getElementById("replayFilePath");
const playerSelect = document.getElementById("playerSelect");
const parseResult = document.getElementById("parseResult");
const timeAdjustment = document.getElementById("timeAdjustment");
const btnApplyOffset = document.getElementById("btnApplyOffset");
const btnSaveJson = document.getElementById("btnSaveJson");
const loading = document.getElementById("loading");

// --- 状態 ---
let currentSessionId = null;
let currentOffset = 0;

// --- ローディング表示 ---
function showLoading() {
    loading.classList.add("active");
}

function hideLoading() {
    loading.classList.remove("active");
}

// --- Step 1: リプレイファイルのアップロード ---
replayFileInput.addEventListener("change", async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    replayFilePath.textContent = file.name;
    replayFilePath.style.color = "#e0e0e0";

    // プレイヤーリストをリセット
    playerSelect.innerHTML = '<option value="-1">-- 読み込み中... --</option>';
    playerSelect.disabled = true;
    parseResult.textContent = "";
    btnSaveJson.hidden = true;
    btnApplyOffset.disabled = true;
    timeAdjustment.value = "0";
    currentOffset = 0;

    const formData = new FormData();
    formData.append("replayFile", file);

    showLoading();
    try {
        const resp = await fetch("/api/upload", {
            method: "POST",
            body: formData,
        });

        if (!resp.ok) {
            const err = await resp.json();
            throw new Error(err.error || "アップロードに失敗しました。");
        }

        const data = await resp.json();
        currentSessionId = data.sessionId;

        // プレイヤーリストを構築
        playerSelect.innerHTML = '<option value="-1">-- プレイヤーを選択してください --</option>';
        data.players.forEach((p) => {
            const opt = document.createElement("option");
            opt.value = p.index;
            opt.textContent = p.label;
            playerSelect.appendChild(opt);
        });
        playerSelect.disabled = false;
        btnSaveJson.hidden = false;

        // 基本情報（プレイヤー未選択）を表示
        await fetchResult(-1, 0);
    } catch (err) {
        parseResult.textContent = "エラー: " + err.message;
    } finally {
        hideLoading();
    }
});

// --- Step 2: プレイヤー選択 ---
playerSelect.addEventListener("change", async () => {
    const playerIndex = parseInt(playerSelect.value, 10);
    currentOffset = parseInt(timeAdjustment.value, 10) || 0;
    btnApplyOffset.disabled = true;

    showLoading();
    try {
        await fetchResult(playerIndex, currentOffset);
    } finally {
        hideLoading();
    }
});

// --- 結果取得 ---
async function fetchResult(playerIndex, offset) {
    if (!currentSessionId) return;

    try {
        const resp = await fetch("/api/result", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                sessionId: currentSessionId,
                playerIndex: playerIndex,
                offset: offset,
            }),
        });

        if (!resp.ok) {
            const err = await resp.json();
            throw new Error(err.error || "結果の取得に失敗しました。");
        }

        const data = await resp.json();
        parseResult.textContent = data.result;
    } catch (err) {
        parseResult.textContent = "エラー: " + err.message;
    }
}

// --- Time Offset ---
timeAdjustment.addEventListener("input", () => {
    const newOffset = parseInt(timeAdjustment.value, 10) || 0;
    btnApplyOffset.disabled = newOffset === currentOffset;
});

timeAdjustment.addEventListener("keydown", async (e) => {
    if (e.key === "Enter") {
        e.preventDefault();
        await applyOffset();
    }
});

btnApplyOffset.addEventListener("click", async () => {
    await applyOffset();
});

async function applyOffset() {
    const newOffset = parseInt(timeAdjustment.value, 10) || 0;
    currentOffset = newOffset;
    btnApplyOffset.disabled = true;

    const playerIndex = parseInt(playerSelect.value, 10);

    showLoading();
    try {
        await fetchResult(playerIndex, currentOffset);
    } finally {
        hideLoading();
    }
}

// --- JSON エクスポート ---
btnSaveJson.addEventListener("click", () => {
    if (!currentSessionId) return;
    // ブラウザのダウンロードを直接トリガー
    window.location.href = `/api/export/${currentSessionId}`;
});
