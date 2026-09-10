/* Local canvas recording. No microphone, desktop capture or upload. */
(() => {
  const panel = document.createElement('div');
  panel.style.cssText = 'position:fixed;left:20px;top:195px;z-index:25;display:flex;gap:8px;align-items:center;background:#091521e8;padding:8px;border:1px solid #b99858;border-radius:6px;font:12px Segoe UI;color:#f0dbaa';
  const button = document.createElement('button');
  button.textContent = 'Record gameplay [C]';
  button.title = 'Records the game canvas at 30 FPS, without HUD or audio. Stops after 60 seconds.';
  const status = document.createElement('span');
  status.setAttribute('role', 'status');
  const download = document.createElement('a');
  download.textContent = 'Save clip';
  download.style.cssText = 'display:none;color:#a7e9ff';
  panel.append(button, status, download);
  document.querySelector('#hud').append(panel);
  let recorder, stream, timer, clock, blobUrl;
  function stop() {
    if (recorder && recorder.state !== 'inactive') recorder.stop();
  }
  function cleanup() {
    clearTimeout(timer);
    clearInterval(clock);
    stream?.getTracks().forEach(track => track.stop());
    button.textContent = 'Record gameplay [C]';
    button.setAttribute('aria-pressed', 'false');
  }
  function toggle() {
    if (button.disabled) return;
    if (recorder && recorder.state !== 'inactive') return stop();
    if (!running || document.querySelector('#modal').open) return;
    try {
      if (!canvas.captureStream || !window.MediaRecorder) throw new Error('Recording unavailable in this browser. Use Chrome.');
      const mimeType = ['video/webm;codecs=vp9', 'video/webm;codecs=vp8', 'video/webm'].find(type => MediaRecorder.isTypeSupported(type));
      if (!mimeType) throw new Error('WebM recording unavailable.');
      stream = canvas.captureStream(30);
      recorder = new MediaRecorder(stream, {mimeType, videoBitsPerSecond: 8000000});
      const chunks = [];
      const filename = 'catmurai-' + new Date().toISOString().replace(/[:.]/g, '-') + '.webm';
      recorder.ondataavailable = event => { if (event.data.size) chunks.push(event.data); };
      let failed = false;
      recorder.onerror = () => { failed = true; status.textContent = 'Recording failed. Try again.'; cleanup(); };
      recorder.onstop = async () => {
        cleanup();
        if (failed) return;
        const clip = new Blob(chunks, {type: mimeType});
        if (!clip.size) { status.textContent = 'No frames captured. Try again.'; return; }
        if (blobUrl) URL.revokeObjectURL(blobUrl);
        blobUrl = URL.createObjectURL(clip);
        download.href = blobUrl;
        download.download = filename;
        download.textContent = 'Save clip';
        download.removeAttribute('target');
        download.style.display = 'inline';
        status.textContent = 'Clip ready · ' + (clip.size / 1048576).toFixed(1) + ' MB';
        button.disabled = true;
        try {
          const response = await fetch('/api/captures', {method: 'POST', headers: {'Content-Type': 'video/webm'}, body: clip});
          if (!response.ok) throw new Error('Local save failed');
          const result = await response.json();
          status.textContent = 'Saved to content/raw';
          download.href = result.path;
          download.textContent = 'Open saved clip';
          download.removeAttribute('download');
          download.target = '_blank';
          download.rel = 'noopener';
        } catch { status.textContent = 'Local save unavailable · use Save clip'; }
        finally { button.disabled = false; }
      };
      recorder.start(1000);
      button.textContent = 'Stop recording [C]';
      button.setAttribute('aria-pressed', 'true');
      status.textContent = 'REC 0s · silent canvas';
      const started = Date.now();
      clock = setInterval(() => status.textContent = 'REC ' + Math.floor((Date.now() - started) / 1000) + 's · silent canvas', 1000);
      timer = setTimeout(stop, 60000);
    } catch (error) { cleanup(); status.textContent = error.message; }
  }
  button.onclick = toggle;
  addEventListener('keydown', event => {
    if (event.repeat || event.ctrlKey || event.altKey || event.metaKey || /INPUT|TEXTAREA|SELECT/.test(event.target.tagName)) return;
    if (event.key.toLowerCase() === 'c') { event.preventDefault(); toggle(); }
  });
  document.addEventListener('visibilitychange', () => { if (document.hidden) stop(); });
  addEventListener('beforeunload', event => {
    if (recorder && recorder.state !== 'inactive') { event.preventDefault(); event.returnValue = ''; }
  });
})();
