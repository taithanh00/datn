document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.card-header-tabs').forEach((tabHeader) => {
    tabHeader.querySelectorAll('.tab-btn').forEach((btn) => {
      btn.addEventListener('click', () => {
        const card = btn.closest('.main-card') || document;
        card.querySelectorAll('.tab-btn').forEach((b) => b.classList.remove('active'));
        card.querySelectorAll('.tab-content').forEach((c) => c.classList.remove('active'));

        btn.classList.add('active');
        const targetId = btn.dataset.tab;
        if (!targetId) return;
        const target = card.querySelector(`#${CSS.escape(targetId)}`);
        if (target) target.classList.add('active');
      });
    });
  });
});

