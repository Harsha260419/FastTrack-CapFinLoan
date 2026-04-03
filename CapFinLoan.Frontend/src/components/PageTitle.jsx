function PageTitle({ title, subtitle }) {
  return (
    <header className="mb-6">
      <h1 className="text-3xl font-bold tracking-tight text-slate-900">{title}</h1>
      {subtitle ? <p className="mt-2 text-slate-600">{subtitle}</p> : null}
    </header>
  )
}

export default PageTitle
