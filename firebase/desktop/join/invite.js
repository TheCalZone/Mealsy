'use strict';
const params = new URLSearchParams(location.hash.slice(1) || location.search);
const code = (params.get('code') || '').trim().toLowerCase();
if (/^[a-f0-9]{48}$/.test(code)) {
  document.getElementById('invite').hidden = false;
  document.getElementById('code').textContent = code;
  document.getElementById('open').href = `mealsy://join?code=${encodeURIComponent(code)}`;
  document.getElementById('copy').onclick = async () => {
    try { await navigator.clipboard.writeText(code); document.getElementById('status').textContent = 'Code copied. Keep it to paste after installing Mealsy.'; }
    catch { document.getElementById('status').textContent = 'Select the code above and copy it, then paste it in Mealsy.'; }
  };
} else if (params.has('code')) document.getElementById('status').textContent = 'This invitation code is invalid. Ask the sender for a fresh code; you can still use Mealsy on your own.';
