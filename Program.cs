using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Drawing;
using System.Windows.Forms;

namespace ControleFinanceiro;

public class Movimento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Data { get; set; } = DateTime.Today;
    public string Conta { get; set; } = "Robert";
    public string Tipo { get; set; } = "Entrada";
    public string Descricao { get; set; } = "";
    public decimal Valor { get; set; }
    public string? Destino { get; set; }
    public string Categoria { get; set; } = "Sem categoria";
    public Guid? RecorrenciaId { get; set; }
    public Guid? ParcelamentoId { get; set; }
    public int NumeroParcela { get; set; }
    public int TotalParcelas { get; set; }
    public bool ParcelaAplicada { get; set; }
}
public class Dados
{
    public Dictionary<string, decimal> Saldos { get; set; } = new() { ["Robert"] = 0, ["David"] = 0, ["Cartão Robert"] = 0 };
    public List<Movimento> Movimentos { get; set; } = new();
    public List<Recorrencia> Recorrencias { get; set; } = new();
    public List<Orcamento> Orcamentos { get; set; } = new();
    public string Tema { get; set; } = "Escuro";
}
static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Tela());
    }
}
public partial class Tela : Form
{
    readonly string arquivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ControleFinanceiro", "dados.json");
    readonly CultureInfo br = CultureInfo.GetCultureInfo("pt-BR");
    Dados dados = new();
    readonly ComboBox meses = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
    readonly DataGridView grade = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, RowHeadersVisible = false };
    readonly Dictionary<string, CartaoPainel> saldos = new();
    readonly Dictionary<string, IndicadorPainel> indicadores = new();
    readonly Label resumo = new() { AutoSize = true };
    readonly Label dica = new() { AutoSize = true };
    readonly Panel areaConteudo = new() { Dock = DockStyle.Fill };
    readonly Dictionary<string, Control> paginas = new();
    readonly Dictionary<string, Button> botoesMenu = new();
    readonly Label tituloPagina = new() { Dock = DockStyle.Top, Height = 54, Font = new Font("Segoe UI", 19, FontStyle.Bold), ForeColor = Color.White };
    readonly string[] contas = { "Robert", "David", "Cartão Robert" };

    static readonly Color Fundo = Color.FromArgb(15, 23, 42);
    static readonly Color Painel = Color.FromArgb(30, 41, 59);
    static readonly Color Texto = Color.FromArgb(241, 245, 249);
    static readonly Color Suave = Color.FromArgb(148, 163, 184);
    static readonly Color Verde = Color.FromArgb(52, 211, 153);
    static readonly Color Vermelho = Color.FromArgb(251, 113, 133);
    static readonly Color Azul = Color.FromArgb(96, 165, 250);
    readonly Label mesTitulo = new() { AutoSize = true, ForeColor = Texto, Font = new Font("Segoe UI", 16, FontStyle.Bold) };

    static Image? LoadArt(string filename)
    {
        try { var path = Path.Combine(AppContext.BaseDirectory, "Assets", filename); return File.Exists(path) ? Image.FromFile(path) : null; }
        catch { return null; }
    }

    public Tela()
    {
        Text = "NEXO  |  Gestão Financeira";
        Size = new Size(1510, 930); MinimumSize = new Size(1130, 730);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10); BackColor = Fundo; ForeColor = Texto;
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "icone.ico")); } catch { }
        Carregar();
        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(32, 25, 32, 22), ColumnCount = 1, RowCount = 6, BackColor = Fundo };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 67));
        CriarEstruturaNavegacao();
        areaConteudo.Controls.Add(raiz);
        paginas["Visão Geral"] = raiz;

        var cabecalho = new Panel { Dock = DockStyle.Fill };
        cabecalho.Controls.Add(new Label { Text = "Visão geral", Font = new Font("Segoe UI", 27, FontStyle.Bold), ForeColor = Texto, Dock = DockStyle.Top, Height = 43 });
        cabecalho.Controls.Add(new Label { Text = "Acompanhe seu patrimônio, movimentações e resultados em um só lugar", Font = new Font("Segoe UI", 10), ForeColor = Suave, Dock = DockStyle.Bottom, Height = 29 });
        raiz.Controls.Add(cabecalho, 0, 0);

        var topo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = new Padding(0, 0, 0, 13) };
        for (int i = 0; i < 3; i++) topo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        for (int i = 0; i < contas.Length; i++)
        {
            var conta = contas[i];
            var cor = i == 0 ? Verde : i == 1 ? Azul : Color.FromArgb(192, 132, 252);
            var caixa = new CartaoPainel { Dock = DockStyle.Fill, BackColor = Painel, Accent = cor, Theme = i == 2 ? "credit" : "bank", BackgroundArt = null, Margin = new Padding(i == 0 ? 0 : 9, 0, i == 2 ? 0 : 9, 9), Padding = new Padding(23, 23, 15, 16) };
            caixa.TitleText = i == 2 ? "CARTÃO DE CRÉDITO  /  ROBERT" : "CONTA  /  " + conta.ToUpperInvariant();
            saldos[conta] = caixa;
            topo.Controls.Add(caixa, i, 0);
        }
        raiz.Controls.Add(topo, 0, 1);

        var faixaIndicadores = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Margin = new Padding(0, 0, 0, 13) };
        for (int i = 0; i < 4; i++) faixaIndicadores.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        string[] titulos = { "SALDO TOTAL", "ENTRADAS DO MÊS", "SAÍDAS DO MÊS", "RESULTADO MENSAL" };
        Color[] cores = { Azul, Verde, Vermelho, Color.FromArgb(192, 132, 252) };
        for (int i = 0; i < titulos.Length; i++)
        {
            var indicador = new IndicadorPainel { Dock = DockStyle.Fill, BackColor = Painel, Accent = cores[i], TitleText = titulos[i], Margin = new Padding(i == 0 ? 0 : 7, 0, i == 3 ? 0 : 7, 0) };
            indicadores[titulos[i]] = indicador;
            faixaIndicadores.Controls.Add(indicador, i, 0);
        }
        raiz.Controls.Add(faixaIndicadores, 0, 2);

        var acoes = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        acoes.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        acoes.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var faixa = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        faixa.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); faixa.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        faixa.Controls.Add(mesTitulo, 0, 0);
        foreach (var nome in DateTimeFormatInfo.GetInstance(br).MonthNames.Take(12).Select((m, i) => $"{i + 1:00}  •  {br.TextInfo.ToTitleCase(m)}")) meses.Items.Add(nome);
        meses.Dock = DockStyle.Fill; meses.BackColor = Painel; meses.ForeColor = Texto; meses.FlatStyle = FlatStyle.Flat;
        meses.SelectedIndex = DateTime.Today.Year == 2026 ? DateTime.Today.Month - 1 : 0;
        meses.SelectedIndexChanged += (_, _) => Atualizar();
        faixa.Controls.Add(meses, 1, 0); acoes.Controls.Add(faixa, 0, 0);
        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 7, 0, 0) };
        Botao(barra, "+  Entrada", () => Novo("Entrada"), Color.FromArgb(6, 95, 70));
        Botao(barra, "−  Saída", () => Novo("Saída"), Color.FromArgb(159, 18, 57));
        Botao(barra, "⇄  Transferir", Transferir, Color.FromArgb(13, 116, 144));
        Botao(barra, "✎  Editar movimentação", Editar, Painel);
        Botao(barra, "◈  Editar saldo", EditarSaldo, Painel);
        Botao(barra, "↓  Exportar JSON", Exportar, Color.FromArgb(30, 64, 175));
        acoes.Controls.Add(barra, 0, 1); raiz.Controls.Add(acoes, 0, 3);

        grade.BorderStyle = BorderStyle.None; grade.BackgroundColor = Painel;
        grade.GridColor = Color.FromArgb(51, 65, 85); grade.EnableHeadersVisualStyles = false;
        grade.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(51, 65, 85);
        grade.ColumnHeadersDefaultCellStyle.ForeColor = Texto;
        grade.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        grade.ColumnHeadersHeight = 45; grade.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grade.DefaultCellStyle.BackColor = Painel; grade.DefaultCellStyle.ForeColor = Texto;
        grade.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 83, 124);
        grade.DefaultCellStyle.SelectionForeColor = Color.White;
        grade.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 48, 68);
        grade.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 83, 124);
        grade.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
        grade.RowTemplate.Height = 46; grade.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grade.Columns.Add("Data", "DATA"); grade.Columns.Add("Conta", "ORIGEM / CONTA"); grade.Columns.Add("Tipo", "TIPO"); grade.Columns.Add("Destino", "DESTINO"); grade.Columns.Add("Descricao", "DESCRIÇÃO"); grade.Columns.Add("Valor", "VALOR");
        grade.Columns["Descricao"]!.FillWeight = 175;
        grade.Columns["Destino"]!.FillWeight = 85;
        grade.Columns["Data"]!.FillWeight = 75;
        grade.Columns["Valor"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        raiz.Controls.Add(grade, 0, 4);
        var rodape = new CartaoPainel { Dock = DockStyle.Fill, BackColor = Painel, Accent = Azul, Margin = new Padding(0, 12, 0, 0), Padding = new Padding(17, 12, 0, 0) };
        resumo.ForeColor = Texto; resumo.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        resumo.Dock = DockStyle.Top; resumo.Height = 27; dica.Text = "DADOS LOCAIS  •  Privacidade e controle  •  Seus registros permanecem neste computador"; dica.ForeColor = Suave; dica.Font = new Font("Segoe UI", 9); dica.Dock = DockStyle.Bottom; dica.Height = 20; rodape.Controls.Add(dica); rodape.Controls.Add(resumo); raiz.Controls.Add(rodape, 0, 5);
        CriarPaginas();
        CriarPaginasExtras();
        ProcessarRecorrencias();
        ProcessarParcelas();
        Navegar("Visão Geral");
        Atualizar();
    }
    void CriarEstruturaNavegacao()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Fundo };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 258));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var lateral = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(11, 18, 33), Padding = new Padding(17, 22, 17, 22) };
        var identidade = new Panel { Dock = DockStyle.Top, Height = 105 };
        identidade.Controls.Add(new Label { Text = "NEXO", Dock = DockStyle.Top, Height = 48, ForeColor = Color.White, Font = new Font("Segoe UI", 28, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(13, 0, 0, 0) });
        identidade.Controls.Add(new Label { Text = "FINANÇAS  /  PESSOAL", Dock = DockStyle.Bottom, Height = 37, ForeColor = Color.FromArgb(117, 136, 165), Font = new Font("Segoe UI", 8, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 0, 0, 0) });
        var divisoria = new Panel { Dock = DockStyle.Top, Height = 24 };
        divisoria.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(35, 49, 71)); e.Graphics.DrawLine(pen, 12, 9, divisoria.Width - 12, 9); };
        var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 9, 0, 0), BackColor = lateral.BackColor };
        string[] nomes = { "Visão Geral", "Movimentações", "Transferências", "Cartão de Crédito", "Planejamento", "Relatórios", "Configurações" };
        string[] icones = { "◫", "☷", "⇄", "▣", "▤", "▥", "⚙" };
        for (int i = 0; i < nomes.Length; i++)
        {
            string nome = nomes[i];
            var b = new Button { Text = $"{icones[i]}     {nome}", Width = 217, Height = 52, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0), Margin = new Padding(0, 0, 0, 7), FlatStyle = FlatStyle.Flat, BackColor = lateral.BackColor, ForeColor = Color.FromArgb(157, 173, 194), Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 44, 66);
            b.Click += (_, _) => Navegar(nome);
            botoesMenu[nome] = b;
            menu.Controls.Add(b);
        }
        var baseLateral = new Panel { Dock = DockStyle.Bottom, Height = 105, BackColor = Color.FromArgb(18, 29, 47), Padding = new Padding(12, 13, 10, 8) };
        baseLateral.Controls.Add(new Label { Text = "●  SISTEMA LOCAL", Dock = DockStyle.Top, Height = 29, ForeColor = Color.FromArgb(71, 211, 170), Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        baseLateral.Controls.Add(new Label { Text = "Seus dados sob seu controle\nVersão 3.0  •  Windows", Dock = DockStyle.Bottom, Height = 42, ForeColor = Color.FromArgb(157, 173, 194), Font = new Font("Segoe UI", 9) });
        lateral.Controls.Add(menu);
        lateral.Controls.Add(baseLateral);
        lateral.Controls.Add(divisoria);
        lateral.Controls.Add(identidade);
        layout.Controls.Add(lateral, 0, 0);
        layout.Controls.Add(areaConteudo, 1, 0);
        Controls.Add(layout);
    }

    Panel NovaPagina(string titulo, string subtitulo)
    {
        var pagina = new Panel { Dock = DockStyle.Fill, BackColor = Fundo, Padding = new Padding(35, 31, 35, 30), Visible = false };
        var cab = new Panel { Dock = DockStyle.Top, Height = 118 };
        cab.Controls.Add(new Label { Text = subtitulo, Dock = DockStyle.Bottom, Height = 34, ForeColor = Suave, Font = new Font("Segoe UI", 10) });
        cab.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Top, Height = 49, ForeColor = Texto, Font = new Font("Segoe UI", 28, FontStyle.Bold) });
        pagina.Controls.Add(cab);
        return pagina;
    }

    void AdicionarAcoes(FlowLayoutPanel barra, params (string titulo, Action acao, Color cor)[] acoes)
    {
        foreach (var (titulo, acao, cor) in acoes) Botao(barra, titulo, acao, cor);
    }

    void CriarPaginas()
    {
        var movimentos = NovaPagina("Movimentações", "Consulte e organize seus lançamentos manuais.");
        var corpoMov = new Panel { Dock = DockStyle.Fill };
        var barraMov = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58, AutoScroll = true, WrapContents = false };
        var gradeMov = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false, BackgroundColor = Painel, BorderStyle = BorderStyle.None, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, EnableHeadersVisualStyles = false, AllowUserToResizeRows = false };
        gradeMov.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(51,65,85); gradeMov.ColumnHeadersDefaultCellStyle.ForeColor = Texto;
        gradeMov.DefaultCellStyle.BackColor = Painel; gradeMov.DefaultCellStyle.ForeColor = Texto; gradeMov.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51,83,124);
        gradeMov.RowTemplate.Height = 42;
        foreach (DataGridViewColumn col in grade.Columns) gradeMov.Columns.Add(col.Name, col.HeaderText);
        gradeMov.Columns["Descricao"]!.FillWeight = 170;
        gradeMov.Columns["Valor"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        AdicionarAcoes(barraMov, ("+ Entrada", () => Novo("Entrada"), Color.FromArgb(6,95,70)), ("− Saída", () => Novo("Saída"), Color.FromArgb(159,18,57)), ("✎ Editar selecionada", () => { if (gradeMov.CurrentRow?.Tag is Guid id) { foreach (DataGridViewRow r in grade.Rows) if (r.Tag is Guid rid && rid == id) { grade.CurrentCell = r.Cells[0]; Editar(); return; } } MessageBox.Show("Selecione uma movimentação no histórico."); }, Painel), ("🗑 Excluir movimentação", () => ExcluirMovimentacao(gradeMov.CurrentRow?.Tag is Guid id ? id : null), Color.FromArgb(153, 27, 27)), ("↓ Exportar JSON", Exportar, Color.FromArgb(30,64,175)));
        // Mantém a seleção da tabela principal sincronizada para edição de registros filtrados.
        gradeMov.SelectionChanged += (_, _) => {
            if (gradeMov.CurrentRow?.Tag is Guid id)
                foreach (DataGridViewRow r in grade.Rows)
                    if (r.Tag is Guid rid && rid == id) { grade.CurrentCell = r.Cells[0]; break; }
        };

        var filtros = new TableLayoutPanel { Dock = DockStyle.Top, Height = 105, ColumnCount = 4, RowCount = 2, Padding = new Padding(0, 5, 0, 8), BackColor = Fundo };
        filtros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        filtros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        filtros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        filtros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        filtros.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        filtros.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var pesquisa = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Pesquisar descrição...", BackColor = Painel, ForeColor = Texto, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 12, 8) };
        ComboBox CriarCombo(string[] opcoes) {
            var combo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Painel, ForeColor = Texto, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 12, 8) };
            combo.Items.AddRange(opcoes); combo.SelectedIndex = 0; return combo;
        }
        var filtroConta = CriarCombo(new[] { "Todas as contas", "Robert", "David", "Cartão Robert" });
        var filtroTipo = CriarCombo(new[] { "Todos os tipos", "Entrada", "Saída", "Transferência" });
        var ordenar = CriarCombo(new[] { "Data: recentes", "Data: antigas", "Valor: maior", "Valor: menor" });
        var campos = new Control[] { pesquisa, filtroConta, filtroTipo, ordenar };
        var titulos = new[] { "PESQUISA", "CONTA", "TIPO", "ORDENAÇÃO" };
        for (int j = 0; j < campos.Length; j++) {
            filtros.Controls.Add(new Label { Text = titulos[j], Dock = DockStyle.Fill, ForeColor = Suave, Font = new Font("Segoe UI", 8, FontStyle.Bold) }, j, 0);
            filtros.Controls.Add(campos[j], j, 1);
        }
        var statusMov = new Label { Dock = DockStyle.Bottom, Height = 35, ForeColor = Suave, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5, 0, 0, 0) };
        void PreencherMov()
        {
            var lista = Filtrados().Where(m =>
                (string.IsNullOrWhiteSpace(pesquisa.Text) || m.Descricao.Contains(pesquisa.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)) &&
                (filtroConta.SelectedIndex == 0 || m.Conta == filtroConta.Text) &&
                (filtroTipo.SelectedIndex == 0 || m.Tipo == filtroTipo.Text));
            lista = ordenar.SelectedIndex switch {
                1 => lista.OrderBy(m => m.Data).ThenBy(m => m.Descricao),
                2 => lista.OrderByDescending(m => m.Valor).ThenByDescending(m => m.Data),
                3 => lista.OrderBy(m => m.Valor).ThenByDescending(m => m.Data),
                _ => lista.OrderByDescending(m => m.Data).ThenBy(m => m.Descricao)
            };
            var resultado = lista.ToList();
            gradeMov.Rows.Clear();
            foreach (var m in resultado)
            {
                int i = gradeMov.Rows.Add(m.Data.ToString("dd/MM/yyyy"), m.Conta, m.Tipo, m.Destino ?? "—", m.Descricao, (m.Tipo == "Saída" ? -m.Valor : m.Valor).ToString("C", br));
                gradeMov.Rows[i].Tag = m.Id;
                gradeMov.Rows[i].Cells["Valor"].Style.ForeColor = m.Tipo == "Transferência" ? Azul : m.Tipo == "Saída" ? Vermelho : Verde;
            }
            gradeMov.ClearSelection();
            statusMov.Text = $"{resultado.Count} movimentação(ões) encontrada(s)  •  Mês: {br.TextInfo.ToTitleCase(br.DateTimeFormat.GetMonthName(meses.SelectedIndex + 1))}  •  Total de entradas: {resultado.Where(m => m.Tipo == "Entrada").Sum(m => m.Valor).ToString("C", br)}  •  Saídas: {resultado.Where(m => m.Tipo == "Saída").Sum(m => m.Valor).ToString("C", br)}";
        }
        pesquisa.TextChanged += (_, _) => PreencherMov();
        filtroConta.SelectedIndexChanged += (_, _) => PreencherMov();
        filtroTipo.SelectedIndexChanged += (_, _) => PreencherMov();
        ordenar.SelectedIndexChanged += (_, _) => PreencherMov();
        corpoMov.Controls.Add(gradeMov);
        corpoMov.Controls.Add(statusMov);
        corpoMov.Controls.Add(filtros);
        corpoMov.Controls.Add(barraMov);
        movimentos.Controls.Add(corpoMov); corpoMov.BringToFront();
        paginas["Movimentações"] = movimentos;
        movimentos.Tag = (Action)PreencherMov;

        var transf = NovaPagina("Transferências", "Movimente valores entre Robert e David, ou de Robert para o cartão.");
        var blocoTransf = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 140, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        blocoTransf.Controls.Add(new Label { Text = "Transferências não contam como receita ou despesa.", Width = 680, Height = 48, ForeColor = Suave, Font = new Font("Segoe UI", 11) });
        AdicionarAcoes(blocoTransf, ("⇄ Nova transferência", Transferir, Color.FromArgb(13,116,144)));
        transf.Controls.Add(blocoTransf); blocoTransf.BringToFront(); paginas["Transferências"] = transf;

        var cartao = NovaPagina("Cartão de Crédito", "Acompanhe o saldo disponível do Cartão Robert.");
        var cartaoArea = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 290, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var cartaoInfo = new Label { Width = 650, Height = 105, ForeColor = Texto, Font = new Font("Segoe UI", 18, FontStyle.Bold) };
        cartaoArea.Controls.Add(cartaoInfo);
        AdicionarAcoes(cartaoArea, ("Editar saldo do cartão", EditarSaldo, Painel), ("Transferir de Robert", Transferir, Color.FromArgb(13,116,144)), ("Planejar pagamentos", NovaCompraParcelada, Painel));
        cartao.Controls.Add(cartaoArea); cartaoArea.BringToFront();
        cartao.Tag = (Action)(() => cartaoInfo.Text = "Disponível no cartão: " + dados.Saldos.GetValueOrDefault("Cartão Robert").ToString("C", br));
        paginas["Cartão de Crédito"] = cartao;

        var rel = NovaPagina("Relatórios", "Resumo financeiro e relatórios anuais.");
        var relInfo = new Label { Dock = DockStyle.Top, Height = 230, ForeColor = Texto, Font = new Font("Segoe UI", 15), Padding = new Padding(10, 20, 0, 0) };
        var barraRel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58 };
        AdicionarAcoes(barraRel, ("↓ Exportar relatório JSON", Exportar, Color.FromArgb(30,64,175)), ("Relatório anual", RelatorioAnual, Painel));
        rel.Controls.Add(relInfo); rel.Controls.Add(barraRel); barraRel.BringToFront();
        rel.Tag = (Action)(() => { var lista = Filtrados().ToList(); relInfo.Text = $"Entradas: {lista.Where(m=>m.Tipo=="Entrada").Sum(m=>m.Valor).ToString("C",br)}\n\nSaídas: {lista.Where(m=>m.Tipo=="Saída").Sum(m=>m.Valor).ToString("C",br)}\n\nTransferências: {lista.Where(m=>m.Tipo=="Transferência").Sum(m=>m.Valor).ToString("C",br)}\n\nRegistros: {lista.Count}"; });
        paginas["Relatórios"] = rel;

        var conf = NovaPagina("Configurações", "Gerencie os saldos e os dados locais do aplicativo.");
        var confArea = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 220, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        confArea.Controls.Add(new Label { Text = "Seus dados ficam neste computador, sem conexão com bancos.\n\nArquivo de dados: " + arquivo, Width = 780, Height = 110, ForeColor = Suave, Font = new Font("Segoe UI", 10) });
        AdicionarAcoes(confArea, ("◈ Editar saldos", EditarSaldo, Painel), ("Abrir pasta dos dados", () => { Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = Path.GetDirectoryName(arquivo)!, UseShellExecute = true }); }, Color.FromArgb(30,64,175)));
        AdicionarAcoes(confArea, ("Backup completo", FazerBackup, Color.FromArgb(6,95,70)), ("Restaurar backup", RestaurarBackup, Color.FromArgb(159,18,57)), ("Alternar tema", AlternarTema, Painel));
        conf.Controls.Add(confArea); confArea.BringToFront(); paginas["Configurações"] = conf;

        foreach (var p in paginas.Values.Where(p => p != paginas["Visão Geral"])) areaConteudo.Controls.Add(p);
    }

    void Navegar(string nome)
    {
        foreach (var item in paginas) item.Value.Visible = item.Key == nome;
        if (paginas.TryGetValue(nome, out var pagina))
        {
            pagina.BringToFront();
            if (pagina.Tag is Action atualizar) atualizar();
        }
        foreach (var item in botoesMenu)
        {
            bool ativo = item.Key == nome;
            item.Value.BackColor = ativo ? Color.FromArgb(35, 56, 85) : Color.FromArgb(11, 18, 33);
            item.Value.ForeColor = ativo ? Color.FromArgb(133, 205, 255) : Color.FromArgb(157, 173, 194);
        }
    }

    void Botao(FlowLayoutPanel painel, string nome, Action acao, Color cor)
    {
        var b = new Button { Text = nome, AutoSize = true, Height = 43, Padding = new Padding(17, 0, 17, 0), Margin = new Padding(0, 0, 10, 0), FlatStyle = FlatStyle.Flat, BackColor = cor, ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = ControlPaint.Light(cor, 0.18f);
        b.Click += (_, _) => acao(); painel.Controls.Add(b);
    }
    void Carregar()
    {
        try { if (File.Exists(arquivo)) dados = JsonSerializer.Deserialize<Dados>(File.ReadAllText(arquivo)) ?? new Dados(); }
        catch (Exception ex) { MessageBox.Show("Não foi possível carregar os dados: " + ex.Message); }
        foreach (var conta in contas) dados.Saldos.TryAdd(conta, 0);
        dados.Recorrencias ??= new(); dados.Orcamentos ??= new();
        foreach (var m in dados.Movimentos) m.Categoria ??= "Sem categoria";
    }
    void Salvar()
    {
        try { SalvarSeguro(); }
        catch (Exception ex) { MessageBox.Show("Erro ao salvar: " + ex.Message); }
    }
    IEnumerable<Movimento> Filtrados() => dados.Movimentos.Where(m => m.Data.Year == DateTime.Today.Year && m.Data.Month == meses.SelectedIndex + 1 && m.Data <= DateTime.Today).OrderByDescending(m => m.Data).ThenBy(m => m.Descricao);
    void Atualizar()
    {
        foreach (var conta in contas) saldos[conta].BalanceText = dados.Saldos.GetValueOrDefault(conta).ToString("C", br);
        var movimentosMes = Filtrados().ToList();
        decimal entradas = movimentosMes.Where(m => m.Tipo == "Entrada").Sum(m => m.Valor);
        decimal saidas = movimentosMes.Where(m => m.Tipo == "Saída").Sum(m => m.Valor);
        decimal resultado = entradas - saidas;
        // Cartão representa crédito disponível, não dinheiro em conta.
        decimal saldoTotal = dados.Saldos.GetValueOrDefault("Robert") + dados.Saldos.GetValueOrDefault("David");
        indicadores["SALDO TOTAL"].ValueText = saldoTotal.ToString("C", br);
        indicadores["ENTRADAS DO MÊS"].ValueText = entradas.ToString("C", br);
        indicadores["SAÍDAS DO MÊS"].ValueText = saidas.ToString("C", br);
        indicadores["RESULTADO MENSAL"].ValueText = resultado.ToString("C", br);
        indicadores["RESULTADO MENSAL"].Accent = resultado < 0 ? Vermelho : Verde;
        grade.Rows.Clear();
        foreach (var m in movimentosMes)
        {
            var i = grade.Rows.Add(m.Data.ToString("dd/MM/yyyy"), m.Conta, m.Tipo, m.Destino ?? "—", m.Descricao, (m.Tipo == "Saída" ? -m.Valor : m.Valor).ToString("C", br));
            grade.Rows[i].Tag = m.Id;
            grade.Rows[i].Cells["Valor"].Style.ForeColor = m.Tipo == "Transferência" ? Azul : m.Tipo == "Saída" ? Vermelho : Verde;
        }
        mesTitulo.Text = "Movimentações  /  " + br.TextInfo.ToTitleCase(br.DateTimeFormat.GetMonthName(meses.SelectedIndex + 1));
        if (paginas.TryGetValue("Movimentações", out var paginaMov) && paginaMov.Visible && paginaMov.Tag is Action atualizarMov) atualizarMov();
        resumo.Text = $"↑ Entradas: {Filtrados().Where(m => m.Tipo == "Entrada").Sum(m => m.Valor).ToString("C", br)}     ↓ Saídas: {Filtrados().Where(m => m.Tipo == "Saída").Sum(m => m.Valor).ToString("C", br)}     ⇄ Transferências: {Filtrados().Where(m => m.Tipo == "Transferência").Sum(m => m.Valor).ToString("C", br)}     •  {Filtrados().Count()} registros";
    }
    void Aplicar(Movimento m, int sinal)
    {
        // Cartão representa valor disponível: entradas aumentam e saídas diminuem.
        if (m.Tipo == "Transferência")
        {
            if (string.IsNullOrWhiteSpace(m.Destino)) return;
            dados.Saldos[m.Conta] = dados.Saldos.GetValueOrDefault(m.Conta) - m.Valor * sinal;
            dados.Saldos[m.Destino] = dados.Saldos.GetValueOrDefault(m.Destino) + m.Valor * sinal;
        }
        else dados.Saldos[m.Conta] = dados.Saldos.GetValueOrDefault(m.Conta) + (m.Tipo == "Entrada" ? 1 : -1) * m.Valor * sinal;
    }
    void Novo(string tipo)
    {
        var m = new Movimento { Tipo = tipo, Data = DateTime.Today };
        if (!DialogoMovimento(m)) return;
        dados.Movimentos.Add(m); Aplicar(m, 1); Salvar(); Atualizar();
    }
    Movimento? Selecionado()
    {
        if (grade.CurrentRow?.Tag is Guid id) return dados.Movimentos.FirstOrDefault(m => m.Id == id);
        MessageBox.Show("Selecione uma movimentação na tabela."); return null;
    }
    void ExcluirMovimentacao(Guid? id)
    {
        if (id is null)
        {
            MessageBox.Show(this, "Selecione uma movimentação no histórico para excluir.", "Nenhuma seleção", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var movimento = dados.Movimentos.FirstOrDefault(m => m.Id == id.Value);
        if (movimento == null)
        {
            MessageBox.Show(this, "A movimentação selecionada não foi encontrada.");
            return;
        }
        var confirmacao = MessageBox.Show(this,
            $"Deseja excluir esta movimentação?\n\n{movimento.Descricao}\n{movimento.Conta} • {movimento.Tipo} • {movimento.Valor.ToString("C", br)}\n\nO saldo das contas será recalculado para desfazer esta operação.",
            "Confirmar exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (confirmacao != DialogResult.Yes) return;
        if (movimento.ParcelamentoId == null || movimento.ParcelaAplicada) Aplicar(movimento, -1);
        dados.Movimentos.Remove(movimento);
        Salvar();
        Atualizar();
    }

    void Editar()
    {
        var original = Selecionado(); if (original == null) return;
        var copia = new Movimento { Id = original.Id, Data = original.Data, Conta = original.Conta, Tipo = original.Tipo, Descricao = original.Descricao, Valor = original.Valor, Destino = original.Destino, Categoria = original.Categoria, RecorrenciaId = original.RecorrenciaId, ParcelamentoId = original.ParcelamentoId, NumeroParcela = original.NumeroParcela, TotalParcelas = original.TotalParcelas, ParcelaAplicada = original.ParcelaAplicada };
        if (!(copia.Tipo == "Transferência" ? DialogoTransferencia(copia) : DialogoMovimento(copia))) return;
        if (original.ParcelamentoId == null || original.ParcelaAplicada) Aplicar(original, -1);
        if (copia.ParcelamentoId == null || copia.ParcelaAplicada) Aplicar(copia, 1);
        dados.Movimentos[dados.Movimentos.IndexOf(original)] = copia; Salvar(); Atualizar();
    }
    bool DialogoMovimento(Movimento m)
    {
        using var f = new Form { Text = "Movimentação", Size = new Size(470, 410), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Fundo, ForeColor = Texto, Font = new Font("Segoe UI", 10) };
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), RowCount = 7, ColumnCount = 2 };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); f.Controls.Add(p);
        
        var conta = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill }; conta.Items.AddRange(contas); conta.SelectedItem = m.Conta;
        var tipo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill }; tipo.Items.AddRange(new object[] { "Entrada", "Saída" }); tipo.SelectedItem = m.Tipo;
        var descricao = new TextBox { Text = m.Descricao, Dock = DockStyle.Fill };
        var categoria = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown };
        categoria.Items.AddRange(Categorias); categoria.Text = m.Categoria ?? "Sem categoria";
        var valor = new NumericUpDown { DecimalPlaces = 2, Maximum = 1000000000, Minimum = 0.01m, Value = Math.Max(0.01m, m.Valor), Dock = DockStyle.Fill };
        void Linha(int linha, string nome, Control controle) { p.Controls.Add(new Label { Text = nome, AutoSize = true }, 0, linha); p.Controls.Add(controle, 1, linha); }
        Linha(0, "Conta", conta); Linha(1, "Tipo", tipo); Linha(2, "Descrição", descricao); Linha(3, "Valor (R$)", valor); Linha(4, "Categoria", categoria);
        var ok = new Button { Text = "Salvar", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 110 }; p.Controls.Add(ok, 1, 6); f.AcceptButton = ok;
        if (f.ShowDialog(this) != DialogResult.OK) return false;
        if (string.IsNullOrWhiteSpace(descricao.Text)) { MessageBox.Show("Informe uma descrição."); return false; }
        m.Data = m.Id == Guid.Empty ? DateTime.Today : m.Data; m.Conta = conta.Text; m.Tipo = tipo.Text; m.Descricao = descricao.Text.Trim(); m.Valor = valor.Value; m.Categoria = string.IsNullOrWhiteSpace(categoria.Text) ? "Sem categoria" : categoria.Text.Trim(); return true;
    }
    void Transferir()
    {
        var m = new Movimento { Tipo = "Transferência", Conta = "Robert", Destino = "David", Data = DateTime.Today };
        if (!DialogoTransferencia(m)) return;
        dados.Movimentos.Add(m); Aplicar(m, 1); Salvar(); Atualizar();
    }
    bool DialogoTransferencia(Movimento m)
    {
        using var f = new Form { Text = "Nova transferência", Size = new Size(480, 415), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Fundo, ForeColor = Texto, Font = new Font("Segoe UI", 10) };
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 9 };
        p.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        for (int i = 1; i < 8; i++) p.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        p.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        f.Controls.Add(p);
        p.Controls.Add(new Label { Text = "⇄   TRANSFERÊNCIA ENTRE CONTAS", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Azul, AutoSize = true }, 0, 0);
        var origem = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, BackColor = Painel, ForeColor = Texto };
        origem.Items.AddRange(new object[] { "Robert", "David" }); origem.SelectedItem = m.Conta == "David" ? "David" : "Robert";
        var destino = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, BackColor = Painel, ForeColor = Texto };
        void AtualizarDestinos()
        {
            var anterior = destino.SelectedItem?.ToString() ?? m.Destino;
            destino.Items.Clear();
            foreach (var c in contas.Where(c => c != origem.Text && (c != "Cartão Robert" || origem.Text == "Robert"))) destino.Items.Add(c);
            destino.SelectedItem = destino.Items.Contains(anterior) ? anterior : destino.Items[0];
        }
        origem.SelectedIndexChanged += (_, _) => AtualizarDestinos(); AtualizarDestinos();
        
        var valor = new NumericUpDown { DecimalPlaces = 2, Minimum = 0.01m, Maximum = 1000000000m, Value = Math.Max(0.01m, m.Valor), Dock = DockStyle.Fill, ThousandsSeparator = true };
        var descricao = new TextBox { Text = string.IsNullOrWhiteSpace(m.Descricao) ? "Transferência entre contas" : m.Descricao, Dock = DockStyle.Fill };
        void Campo(int linha, string rotulo, Control controle)
        {
            var linhaPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            linhaPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); linhaPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            linhaPanel.Controls.Add(new Label { Text = rotulo, ForeColor = Suave, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            linhaPanel.Controls.Add(controle, 1, 0); p.Controls.Add(linhaPanel, 0, linha);
        }
        Campo(1, "De", origem); Campo(2, "Para", destino); Campo(3, "Valor (R$)", valor); Campo(4, "Descrição", descricao);
        p.Controls.Add(new Label { Text = "O cartão Robert só recebe de Robert e nunca pode ser origem.", ForeColor = Suave, AutoSize = true }, 0, 6);
        var botao = new Button { Text = "CONFIRMAR TRANSFERÊNCIA", BackColor = Color.FromArgb(13, 116, 144), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Cursor = Cursors.Hand };
        botao.FlatAppearance.BorderSize = 0; p.Controls.Add(botao, 0, 7);
        botao.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(descricao.Text)) { MessageBox.Show(f, "Informe uma descrição."); return; }
            if (origem.Text == destino.Text || origem.Text == "Cartão Robert" || (destino.Text == "Cartão Robert" && origem.Text != "Robert")) { MessageBox.Show(f, "Transferência não permitida."); return; }
            m.Conta = origem.Text; m.Destino = destino.Text; m.Valor = valor.Value; m.Descricao = descricao.Text.Trim(); m.Tipo = "Transferência";
            f.DialogResult = DialogResult.OK; f.Close();
        };
        return f.ShowDialog(this) == DialogResult.OK;
    }
    void EditarSaldo()
    {
        using var f = new Form { Text = "Editar saldo", Size = new Size(360, 200), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, BackColor = Fundo, ForeColor = Texto, Font = new Font("Segoe UI", 10) };
        var p = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), FlowDirection = FlowDirection.TopDown };
        var conta = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 290 }; conta.Items.AddRange(contas); conta.SelectedIndex = 0;
        var valor = new NumericUpDown { DecimalPlaces = 2, Minimum = -1000000000, Maximum = 1000000000, Width = 290 };
        conta.SelectedIndexChanged += (_, _) => valor.Value = Math.Clamp(dados.Saldos.GetValueOrDefault(conta.Text), valor.Minimum, valor.Maximum);
        valor.Value = dados.Saldos.GetValueOrDefault(conta.Text);
        var ok = new Button { Text = "Salvar saldo", DialogResult = DialogResult.OK, Width = 150 };
        p.Controls.Add(conta); p.Controls.Add(valor); p.Controls.Add(ok); f.Controls.Add(p); f.AcceptButton = ok;
        if (f.ShowDialog(this) != DialogResult.OK) return;
        dados.Saldos[conta.Text] = valor.Value; Salvar(); Atualizar();
    }
    void Exportar()
    {
        var movimentos = Filtrados().ToList();
        int ano = DateTime.Today.Year;
        int mes = meses.SelectedIndex + 1;
        using var s = new SaveFileDialog
        {
            Title = "Exportar movimentações em JSON",
            Filter = "Arquivo JSON (*.json)|*.json",
            DefaultExt = "json",
            AddExtension = true,
            FileName = $"movimentacoes_{ano}_{mes:00}.json"
        };
        if (s.ShowDialog(this) != DialogResult.OK) return;

        var exportacao = new
        {
            Ano = ano,
            Mes = mes,
            TotalRegistros = movimentos.Count,
            Resumo = new
            {
                Entradas = movimentos.Where(m => m.Tipo == "Entrada").Sum(m => m.Valor),
                Saidas = movimentos.Where(m => m.Tipo == "Saída").Sum(m => m.Valor),
                Transferencias = movimentos.Where(m => m.Tipo == "Transferência").Sum(m => m.Valor)
            },
            Movimentacoes = movimentos
        };
        try
        {
            var json = JsonSerializer.Serialize(exportacao, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(s.FileName, json, new UTF8Encoding(false));
            MessageBox.Show(this, "Mês exportado em JSON com sucesso!", "Exportação concluída");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Erro ao exportar JSON: " + ex.Message);
        }
    }

}

public class CartaoPainel : Panel
{
    public Color Accent { get; set; } = Color.MediumSeaGreen;
    public string Theme { get; set; } = "bank";
    public Image? BackgroundArt { get; set; }
    public string TitleText { get; set; } = "";
    private string balanceText = "R$ 0,00";
    public string BalanceText
    {
        get => balanceText;
        set { balanceText = value; Invalidate(); }
    }

    public CartaoPainel() { DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var balanceFont = new Font("Segoe UI", 25, FontStyle.Bold);
        using var shadow = new SolidBrush(Color.FromArgb(175, 5, 12, 24));
        using var accentBrush = new SolidBrush(Accent);
        using var whiteBrush = new SolidBrush(Color.FromArgb(241, 245, 249));
        var titleRect = new RectangleF(45, 20, Math.Max(1, Width - 64), 31);
        var balanceRect = new RectangleF(25, Math.Max(62, Height - 88), Math.Max(1, Width - 50), 61);
        using var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        e.Graphics.DrawString(TitleText, titleFont, shadow, titleRect.X + 1, titleRect.Y + 2, fmt);
        e.Graphics.DrawString(TitleText, titleFont, accentBrush, titleRect, fmt);
        e.Graphics.DrawString(BalanceText, balanceFont, shadow, balanceRect.X + 2, balanceRect.Y + 2, fmt);
        e.Graphics.DrawString(BalanceText, balanceFont, whiteBrush, balanceRect, fmt);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        var r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        int d = 22;
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        using var grad = new System.Drawing.Drawing2D.LinearGradientBrush(r,
            Color.FromArgb(34, 52, 75), Color.FromArgb(23, 36, 57), 15f);
        g.FillPath(grad, path);
        var state = g.Save();
        g.SetClip(path);
        using (var halo = new SolidBrush(Color.FromArgb(24, Accent)))
            g.FillEllipse(halo, Width - 125, -95, 225, 225);
        using (var halo2 = new Pen(Color.FromArgb(43, Accent), 1.4f))
        {
            g.DrawEllipse(halo2, Width - 145, -85, 240, 240);
            g.DrawEllipse(halo2, Width - 113, -55, 178, 178);
        }
        using (var mark = new SolidBrush(Accent)) g.FillEllipse(mark, 26, 27, 9, 9);
        g.Restore(state);
        using var border = new Pen(Color.FromArgb(53, 73, 101));
        g.DrawPath(border, path);
    }

}

// Cartões de indicadores do dashboard; não alteram os dados financeiros.
public class IndicadorPainel : Panel
{
    public string TitleText { get; set; } = "";
    private string valor = "R$ 0,00";
    public string ValueText { get => valor; set { valor = value; Invalidate(); } }
    private Color accent = Color.CornflowerBlue;
    public Color Accent { get => accent; set { accent = value; Invalidate(); } }
    public IndicadorPainel() { DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        var r = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        int d = 16;
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        using var bg = new SolidBrush(Color.FromArgb(25, 39, 59));
        using var border = new Pen(Color.FromArgb(53, 73, 101));
        e.Graphics.FillPath(bg, path);
        e.Graphics.DrawPath(border, path);
        using var accentBrush = new SolidBrush(Accent);
        e.Graphics.FillEllipse(accentBrush, 18, 20, 8, 8);
        using var labelFont = new Font("Segoe UI", 9, FontStyle.Bold);
        using var valueFont = new Font("Segoe UI", Width < 220 ? 16 : 20, FontStyle.Bold);
        using var softBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
        using var textBrush = new SolidBrush(Color.FromArgb(241, 245, 249));
        using var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        e.Graphics.DrawString(TitleText, labelFont, softBrush, new RectangleF(33, 15, Math.Max(1, Width - 48), 26), fmt);
        e.Graphics.DrawString(ValueText, valueFont, textBrush, new RectangleF(18, 52, Math.Max(1, Width - 36), 45), fmt);
    }
}