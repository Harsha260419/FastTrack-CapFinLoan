function PageCard({ title, children }) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <h1 className="mb-2 text-2xl font-semibold">{title}</h1>
      <div className="text-slate-600">{children}</div>
    </section>
  )
}

export default PageCard
