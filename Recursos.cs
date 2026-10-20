using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Text.Json;
using System.Collections.Generic;

namespace ControleFinanceiro;

public class Recorrencia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Conta { get; set; } = "Robert";
    public string Descricao { get; set; } = "";
    public string Categoria { get; set; } = "Contas";
    public decimal Valor { get; set; }
    public int Dia { get; set; } = 1;
    public DateTime Inicio { get; set; } = DateTime.Today;
    public bool Ativa { get; set; } = true;
}
public class Orcamento
{
    public string Conta { get; set; } = "Robert";
    public string Categoria { get; set; } = "Todas";
    public int Ano { get; set; } = DateTime.Today.Year;
    public int Mes { get; set; } = DateTime.Today.Month;
    public decimal Limite { get; set; }
}

public partial class Tela
{
    static readonly object[] Categorias = { "Sem categoria", "Alimentação", "Moradia", "Contas", "Transporte", "Saúde", "Animais", "Lazer", "Compras", "Assinaturas", "Educação", "Outros" };
    void SalvarSeguro()
    {
        var dir = Path.GetDirectoryName(arquivo)!;
        Directory.CreateDirectory(dir);
        string tmp = arquivo + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true }));
        if (File.Exists(arquivo)) File.Replace(tmp, arquivo, arquivo + ".bak");
        else File.Move(tmp, arquivo);
    }
    void FazerBackup()
    {
        using var s = new SaveFileDialog { Filter = "Backup JSON (*.json)|*.json", FileName = $"ControleFinanceiro_backup_{DateTime.Now:yyyyMMdd_HHmm}.json", DefaultExt = "json", AddExtension = true };
        if (s.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllText(s.FileName, JsonSerializer.Serialize(dados, new JsonSerializerOptions { WriteIndented = true })); MessageBox.Show(this, "Backup completo salvo."); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Falha no backup"); }
    }
    void RestaurarBackup()
    {
        using var s = new OpenFileDialog { Filter = "Backup JSON (*.json)|*.json" };
        if (s.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var novo = JsonSerializer.Deserialize<Dados>(File.ReadAllText(s.FileName)) ?? throw new Exception("Backup vazio.");
            if (novo.Saldos == null || novo.Movimentos == null || contas.Any(c => !novo.Saldos.ContainsKey(c))) throw new Exception("Estrutura de dados inválida.");
            if (MessageBox.Show(this, "Restaurar substituirá os dados atuais. Um backup de segurança será criado. Continuar?", "Restaurar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
            if (File.Exists(arquivo)) File.Copy(arquivo, arquivo + ".antes_restauracao_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json");
            dados = novo; dados.Recorrencias ??= new(); dados.Orcamentos ??= new(); Salvar(); Atualizar(); MessageBox.Show(this, "Backup restaurado.");
        }
        catch (Exception ex) { MessageBox.Show(this, "Não foi possível restaurar: " + ex.Message); }
    }
    void ProcessarRecorrencias()
    {
        bool alterou = false;
        foreach (var r in dados.Recorrencias.Where(x => x.Ativa && x.Valor > 0).ToList())
        {
            var inicio = new DateTime(r.Inicio.Year, r.Inicio.Month, 1);
            for (var mes = inicio; mes <= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); mes = mes.AddMonths(1))
            {
                var data = new DateTime(mes.Year, mes.Month, Math.Min(Math.Clamp(r.Dia,1,31), DateTime.DaysInMonth(mes.Year,mes.Month)));
                if (data < r.Inicio || data > DateTime.Today || dados.Movimentos.Any(m => m.RecorrenciaId == r.Id && m.Data.Date == data)) continue;
                var mov = new Movimento { Conta = r.Conta, Tipo = "Saída", Descricao = r.Descricao, Valor = r.Valor, Categoria = r.Categoria, Data = data, RecorrenciaId = r.Id };
                dados.Movimentos.Add(mov); Aplicar(mov, 1); alterou = true;
            }
        }
        if (alterou) Salvar();
    }
    void ProcessarParcelas()
    {
        bool alterou=false;
        foreach(var m in dados.Movimentos.Where(x=>x.ParcelamentoId!=null && !x.ParcelaAplicada && x.Data.Date<=DateTime.Today)){
            Aplicar(m,1);m.ParcelaAplicada=true;alterou=true;
        }
        if(alterou)Salvar();
    }
    void CriarPaginasExtras()
    {
        var pagina = NovaPagina("Planejamento", "Orçamentos mensais, despesas recorrentes e parcelamentos.");
        var acoes = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58, WrapContents = false, AutoScroll = true };
        AdicionarAcoes(acoes, ("+ Orçamento", NovoOrcamento, Painel), ("+ Despesa recorrente", NovaRecorrencia, Color.FromArgb(6,95,70)), ("+ Planejar pagamentos", NovaCompraParcelada, Color.FromArgb(30,64,175)), ("Gerenciar recorrências", GerenciarRecorrencias, Painel));
        var info = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Painel, ForeColor = Texto, Font = new Font("Consolas", 11), BorderStyle = BorderStyle.None };
        pagina.Controls.Add(info); pagina.Controls.Add(acoes); acoes.BringToFront();
        pagina.Tag = (Action)(() => {
            var linhas = new List<string> { "ORÇAMENTOS DO MÊS", "" };
            foreach (var o in dados.Orcamentos.Where(o => o.Ano == DateTime.Today.Year && o.Mes == meses.SelectedIndex+1)) {
                decimal gasto = dados.Movimentos.Where(m => m.Tipo == "Saída" && m.Conta == o.Conta && m.Data.Year == o.Ano && m.Data.Month == o.Mes && (o.Categoria == "Todas" || m.Categoria == o.Categoria)).Sum(m => m.Valor);
                linhas.Add($"{o.Conta} / {o.Categoria}: {gasto:C} de {o.Limite:C}  ({(gasto > o.Limite ? "LIMITE ULTRAPASSADO" : "dentro do limite")})");
            }
            linhas.AddRange(new[] { "", "DESPESAS RECORRENTES", "" });
            foreach (var r in dados.Recorrencias) linhas.Add($"{(r.Ativa ? "ATIVA" : "PAUSADA")} | dia {r.Dia:00} | {r.Conta} | {r.Descricao} | {r.Valor:C}");
            linhas.AddRange(new[] { "", "PAGAMENTOS FUTUROS (não debitados)", "" });
            foreach (var m in dados.Movimentos.Where(m => m.ParcelamentoId != null && m.Data > DateTime.Today).OrderBy(m => m.Data)) linhas.Add($"{m.Data:MM/yyyy} | {m.Descricao} | {m.Valor:C}");
            info.Lines = linhas.ToArray();
        });
        paginas["Planejamento"] = pagina; areaConteudo.Controls.Add(pagina);
        var rel = paginas["Relatórios"] as Panel;
        if (rel != null) {
            var chart = new GraficoFinanceiro { Dock = DockStyle.Bottom, Height = 245, BackColor = Painel };
            rel.Controls.Add(chart);
            var antiga = rel.Tag as Action;
            rel.Tag = (Action)(() => { antiga?.Invoke(); chart.Movimentos = dados.Movimentos.Where(m => m.Data.Year == DateTime.Today.Year).ToList(); chart.Invalidate(); });
        }
    }
    Form Dialogo(string titulo, int altura = 340) => new Form { Text = titulo, Size = new Size(460, altura), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, BackColor = Fundo, ForeColor = Texto, Font = new Font("Segoe UI", 10) };
    (TableLayoutPanel p, Button ok) Formulario(Form f)
    {
        var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(20), AutoScroll = true };
        p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); f.Controls.Add(p);
        var ok = new Button { Text = "Salvar", Height = 35, Width = 120, DialogResult = DialogResult.OK, BackColor = Azul, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
        f.AcceptButton = ok; return (p,ok);
    }
    void Campo(TableLayoutPanel p, string nome, Control c) { int i = p.RowCount++; p.RowStyles.Add(new RowStyle(SizeType.Absolute, 43)); p.Controls.Add(new Label { Text = nome, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Suave },0,i); c.Dock = DockStyle.Fill; p.Controls.Add(c,1,i); }
    ComboBox Escolhas(IEnumerable<string> opcoes) { var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList }; c.Items.AddRange(opcoes.Cast<object>().ToArray()); c.SelectedIndex = 0; return c; }
    NumericUpDown Dinheiro() => new NumericUpDown { DecimalPlaces = 2, Maximum = 1000000000, Minimum = 0.01m, ThousandsSeparator = true, Value = 1 };
    void Finalizar(TableLayoutPanel p, Button b) { int i=p.RowCount++; p.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); p.Controls.Add(b,1,i); }
    void NovoOrcamento()
    {
        using var f=Dialogo("Novo orçamento"); var (p,ok)=Formulario(f);
        var conta=Escolhas(contas); var categoria=Escolhas(new[]{"Todas"}.Concat(Categorias.Cast<string>())); var valor=Dinheiro();
        Campo(p,"Conta",conta); Campo(p,"Categoria",categoria); Campo(p,"Limite mensal",valor); Finalizar(p,ok);
        if(f.ShowDialog(this)!=DialogResult.OK)return;
        dados.Orcamentos.RemoveAll(o=>o.Conta==conta.Text && o.Categoria==categoria.Text && o.Mes==meses.SelectedIndex+1 && o.Ano==DateTime.Today.Year);
        dados.Orcamentos.Add(new Orcamento { Conta=conta.Text,Categoria=categoria.Text,Limite=valor.Value,Mes=meses.SelectedIndex+1,Ano=DateTime.Today.Year }); Salvar(); Navegar("Planejamento");
    }
    void NovaRecorrencia()
    {
        using var f=Dialogo("Nova despesa recorrente",390); var(p,ok)=Formulario(f);
        var conta=Escolhas(contas); var categoria=Escolhas(Categorias.Cast<string>()); var descricao=new TextBox(); var valor=Dinheiro(); var dia=new NumericUpDown{Minimum=1,Maximum=31,Value=DateTime.Today.Day};
        Campo(p,"Conta",conta);Campo(p,"Descrição",descricao);Campo(p,"Categoria",categoria);Campo(p,"Valor",valor);Campo(p,"Dia do mês",dia);Finalizar(p,ok);
        if(f.ShowDialog(this)!=DialogResult.OK)return;
        if(string.IsNullOrWhiteSpace(descricao.Text)){MessageBox.Show("Informe uma descrição.");return;}
        dados.Recorrencias.Add(new Recorrencia{Conta=conta.Text,Descricao=descricao.Text.Trim(),Categoria=categoria.Text,Valor=valor.Value,Dia=(int)dia.Value,Inicio=DateTime.Today}); ProcessarRecorrencias(); Salvar(); Atualizar(); Navegar("Planejamento");
    }
    void GerenciarRecorrencias()
    {
        using var f=Dialogo("Pausar ou retomar recorrência",420);
        var lista=new ListBox { Dock=DockStyle.Fill,BackColor=Painel,ForeColor=Texto };
        foreach(var r in dados.Recorrencias)lista.Items.Add($"{(r.Ativa?"ATIVA":"PAUSADA")} | {r.Descricao} | {r.Conta} | {r.Valor:C}");
        var botao=new Button{Text="Alternar ativa/pausada",Dock=DockStyle.Bottom,Height=45}; f.Controls.Add(lista);f.Controls.Add(botao);
        botao.Click+=(_,_)=>{if(lista.SelectedIndex<0)return;var r=dados.Recorrencias[lista.SelectedIndex];r.Ativa=!r.Ativa;Salvar();f.Close();Navegar("Planejamento");}; f.ShowDialog(this);
    }
    void NovaCompraParcelada()
    {
        using var f = Dialogo("Planejar pagamentos por mês", 590);
        f.Width = 600;
        f.MinimumSize = new Size(560, 480);
        f.FormBorderStyle = FormBorderStyle.Sizable;
        f.MaximizeBox = true;
        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(20, 16, 20, 15), BackColor = Fundo };
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 153));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        f.Controls.Add(raiz);
        var campos = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++) campos.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        var conta = Escolhas(contas);
        var descricao = new TextBox { BackColor = Painel, ForeColor = Texto };
        var categoria = Escolhas(Categorias.Cast<string>());
        Campo(campos, "Conta", conta);
        Campo(campos, "Descrição", descricao);
        Campo(campos, "Categoria", categoria);
        raiz.Controls.Add(campos, 0, 0);
        raiz.Controls.Add(new Label {
            Text = "MÊS DE PAGAMENTO                                  VALOR (R$)",
            Dock = DockStyle.Fill, ForeColor = Suave,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        var lista = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Fundo };
        raiz.Controls.Add(lista, 0, 2);
        var pagamentos = new List<(DateTimePicker mes, NumericUpDown valor, Panel linha)>();
        void AdicionarMes(DateTime data, decimal quantia)
        {
            var linha = new Panel { Width = Math.Max(465, lista.ClientSize.Width - 25), Height = 45, Margin = new Padding(0, 0, 0, 5) };
            var mes = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MMMM/yyyy", Value = data, Width = 220, Location = new Point(0, 5) };
            var valor = new NumericUpDown { DecimalPlaces = 2, Minimum = 0.01m, Maximum = 1000000000m, ThousandsSeparator = true, Value = Math.Max(0.01m, quantia), Width = 155, Location = new Point(232, 5) };
            var remover = new Button { Text = "Excluir", Width = 70, Height = 28, Location = new Point(396, 5), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(127, 29, 29), ForeColor = Color.White };
            remover.FlatAppearance.BorderSize = 0;
            linha.Controls.Add(mes); linha.Controls.Add(valor); linha.Controls.Add(remover);
            lista.Controls.Add(linha);
            pagamentos.Add((mes, valor, linha));
            remover.Click += (_, _) => {
                if (pagamentos.Count == 1) { MessageBox.Show(f, "Mantenha pelo menos um mês de pagamento."); return; }
                pagamentos.RemoveAll(x => ReferenceEquals(x.linha, linha));
                lista.Controls.Remove(linha); linha.Dispose(); AtualizarTotal();
            };
            valor.ValueChanged += (_, _) => AtualizarTotal();
            AtualizarTotal();
        }
        var rodape = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        rodape.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        rodape.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        rodape.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        rodape.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        var adicionar = new Button { Text = "+ Adicionar mês", Dock = DockStyle.Fill, BackColor = Painel, ForeColor = Texto, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 12, 4) };
        var total = new Label { Text = "Total previsto: R$ 0,00", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, ForeColor = Verde, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        var aviso = new Label { Text = "Os valores futuros serão debitados no início do mês cadastrado.", Dock = DockStyle.Fill, ForeColor = Suave, AutoEllipsis = true };
        var salvar = new Button { Text = "Salvar planejamento", Dock = DockStyle.Fill, BackColor = Azul, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 2, 0, 0) };
        rodape.Controls.Add(adicionar, 0, 0); rodape.Controls.Add(total, 1, 0);
        rodape.Controls.Add(aviso, 0, 1); rodape.Controls.Add(salvar, 1, 1);
        raiz.Controls.Add(rodape, 0, 3);
        void AtualizarTotal() => total.Text = "Total previsto: " + pagamentos.Sum(x => x.valor.Value).ToString("C", br);
        AdicionarMes(DateTime.Today, 1);
        adicionar.Click += (_, _) => {
            var ultima = pagamentos.Max(x => new DateTime(x.mes.Value.Year, x.mes.Value.Month, 1));
            AdicionarMes(ultima.AddMonths(1), 1);
        };
        salvar.Click += (_, _) => {
            if (string.IsNullOrWhiteSpace(descricao.Text)) { MessageBox.Show(f, "Informe uma descrição."); return; }
            var itens = pagamentos.Select(x => (mes: new DateTime(x.mes.Value.Year, x.mes.Value.Month, 1), valor: x.valor.Value)).OrderBy(x => x.mes).ToList();
            if (itens.GroupBy(x => x.mes).Any(g => g.Count() > 1)) { MessageBox.Show(f, "Cada mês deve aparecer apenas uma vez."); return; }
            var mesAtual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (itens.Any(x => x.mes < mesAtual)) { MessageBox.Show(f, "Não é possível programar pagamentos em meses anteriores."); return; }
            var id = Guid.NewGuid();
            for (int i = 0; i < itens.Count; i++) {
                var item = itens[i];
                var data = item.mes == mesAtual ? DateTime.Today : item.mes;
                var m = new Movimento {
                    Conta = conta.Text, Tipo = "Saída", Descricao = descricao.Text.Trim(), Categoria = categoria.Text,
                    Valor = item.valor, Data = data, ParcelamentoId = id, NumeroParcela = i + 1, TotalParcelas = itens.Count
                };
                dados.Movimentos.Add(m);
                if (m.Data.Date <= DateTime.Today) { Aplicar(m, 1); m.ParcelaAplicada = true; }
            }
            Salvar(); Atualizar(); f.DialogResult = DialogResult.OK; f.Close(); Navegar("Planejamento");
        };
        f.ShowDialog(this);
    }
    void RelatorioAnual()
    {
        int ano=DateTime.Today.Year;
        var linhas=new List<string>{"RELATÓRIO ANUAL - "+ano,""};
        for(int mes=1;mes<=12;mes++){
            var lista=dados.Movimentos.Where(m=>m.Data.Year==ano&&m.Data.Month==mes&&m.Data<=DateTime.Today).ToList();
            var e=lista.Where(m=>m.Tipo=="Entrada").Sum(m=>m.Valor);var s=lista.Where(m=>m.Tipo=="Saída").Sum(m=>m.Valor);
            linhas.Add($"{mes:00} | Entradas: {e:C} | Saídas: {s:C} | Resultado: {e-s:C}");
        }
        using var f=Dialogo("Relatório anual",570);var t=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=Painel,ForeColor=Texto,Font=new Font("Consolas",10),Lines=linhas.ToArray()};f.Controls.Add(t);f.ShowDialog(this);
    }
    void AlternarTema()
    {
        dados.Tema=dados.Tema=="Claro"?"Escuro":"Claro";Salvar();
        var claro=dados.Tema=="Claro";
        // Atualiza a paleta de controles padrão; cartões personalizados preservam seus acentos.
        void Pintar(Control c){
            if(c is TextBox or ComboBox or DataGridView or NumericUpDown){c.BackColor=claro?Color.White:Painel;c.ForeColor=claro?Color.FromArgb(25,35,55):Texto;}
            else if(c is Panel or TableLayoutPanel or FlowLayoutPanel){c.BackColor=claro?Color.FromArgb(235,240,248):Fundo;}
            if(c is Label)c.ForeColor=claro?Color.FromArgb(35,45,65):Texto;
            foreach(Control filho in c.Controls)Pintar(filho);
        }
        Pintar(this); MessageBox.Show(this,"Tema "+dados.Tema+" ativado.");
    }
}

public class GraficoFinanceiro : Panel
{
    public List<Movimento> Movimentos {get;set;}=new();
    public GraficoFinanceiro(){DoubleBuffered=true;}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g=e.Graphics;g.Clear(BackColor);
        using var titulo=new Font("Segoe UI",11,FontStyle.Bold);using var fonte=new Font("Segoe UI",8);
        g.DrawString("Entradas e saídas por mês",titulo,Brushes.White,14,8);
        int w=Math.Max(10,(Width-50)/12),h=Math.Max(20,Height-75);
        decimal max=1;
        for(int m=1;m<=12;m++){var a=Movimentos.Where(x=>x.Data.Month==m&&x.Data<=DateTime.Today);max=Math.Max(max,Math.Max(a.Where(x=>x.Tipo=="Entrada").Sum(x=>x.Valor),a.Where(x=>x.Tipo=="Saída").Sum(x=>x.Valor)));}
        for(int m=1;m<=12;m++){
            var a=Movimentos.Where(x=>x.Data.Month==m&&x.Data<=DateTime.Today);
            decimal en=a.Where(x=>x.Tipo=="Entrada").Sum(x=>x.Valor),sa=a.Where(x=>x.Tipo=="Saída").Sum(x=>x.Valor);
            int x=25+(m-1)*w, bw=Math.Max(3,(w-10)/2);
            int eh=(int)(h*en/max),sh=(int)(h*sa/max);
            using var verde=new SolidBrush(Color.FromArgb(52,211,153));using var vermelho=new SolidBrush(Color.FromArgb(251,113,133));
            g.FillRectangle(verde,x,Height-40-eh,bw,eh);g.FillRectangle(vermelho,x+bw+2,Height-40-sh,bw,sh);
            g.DrawString(m.ToString("00"),fonte,Brushes.White,x,Height-32);
        }
        g.DrawString("Verde: entradas    Rosa: saídas",fonte,Brushes.White,14,Height-15);
    }
}