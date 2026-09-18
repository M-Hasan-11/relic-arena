const realms = {
  azure: { title: 'Azure Sanctuary', kicker: 'THE FIRST TRIAL', boss: 'THE GATE WARDEN', copy: "A crystal temple beneath a mountain sky. Learn the rhythm of combat, then face the Gate Warden's crushing slam and charge." },
  ember: { title: 'Ember Citadel', kicker: 'INTO THE FIRE', boss: 'CINDER TYRANT', copy: 'Warm dusk falls over a fortified arena. Use the barricades, dodge a fan of projectiles, and break through the Cinder Tyrant\'s relentless attacks.' },
  astral: { title: 'Astral Summit', kicker: 'THE FINAL TRIAL', boss: 'THE ASTRAL KING', copy: 'Beneath a star-filled sky, the last guardian awaits. Face stronger attacks and a larger entourage in the final battle for the realms.' }
};
const tabs = [...document.querySelectorAll('[role="tab"]')];
function selectRealm(tab) {
  const name = tab.dataset.realm;
  const realm = realms[name];
  tabs.forEach(item => { item.setAttribute('aria-selected', String(item === tab)); item.tabIndex = item === tab ? 0 : -1; });
  document.querySelector('#realm-panel').setAttribute('aria-labelledby', tab.id);
  document.querySelector('#realm-title').textContent = realm.title;
  document.querySelector('#realm-kicker').textContent = realm.kicker;
  document.querySelector('#realm-copy').textContent = realm.copy;
  document.querySelector('#realm-boss').textContent = `GUARDIAN / ${realm.boss}`;
  const img = document.querySelector('#realm-image');
  img.src = `/media/${name}.png`;
  img.alt = `${realm.boss} boss battle in ${realm.title}`;
}
tabs.forEach((tab, index) => {
  tab.addEventListener('click', () => selectRealm(tab));
  tab.addEventListener('keydown', event => {
    let next;
    if (event.key === 'ArrowRight') next = (index + 1) % tabs.length;
    if (event.key === 'ArrowLeft') next = (index + tabs.length - 1) % tabs.length;
    if (event.key === 'Home') next = 0;
    if (event.key === 'End') next = tabs.length - 1;
    if (next !== undefined) { event.preventDefault(); tabs[next].focus(); selectRealm(tabs[next]); }
  });
});
document.querySelector('#copy-checksum').addEventListener('click', async () => {
  const status = document.querySelector('#copy-status');
  try { await navigator.clipboard.writeText(document.querySelector('#checksum').textContent); status.textContent = 'Copied!'; }
  catch { status.textContent = 'Select and copy the checksum above.'; }
});
