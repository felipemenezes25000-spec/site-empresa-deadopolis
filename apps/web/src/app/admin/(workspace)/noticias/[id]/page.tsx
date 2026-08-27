import { NewsEditor } from "@/components/admin/news-editor";

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <>
    <div className="admin-heading"><div><h1>Editar notícia</h1><p>Atualize o conteúdo com controle de versão e siga o workflow editorial governado.</p></div></div>
    <NewsEditor articleId={id} />
  </>;
}
