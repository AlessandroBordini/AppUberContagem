using AppUberContagem.Models;
using Microsoft.Maui.Storage;
using System.Globalization;
using System.Text.Json;
using AppUberContagem.Helpers;

namespace AppUberContagem;

public partial class DetalheViagemPage : ContentPage
{
    private readonly string _viagemId;
    private List<Viagem> _listaViagens = new();
    private Viagem? _viagemAtual;

    public DetalheViagemPage(string viagemId)
    {
        InitializeComponent();
        _viagemId = viagemId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Gerencia a visibilidade do banner baseado no status Premium do usuário
        bool isPremium = await PremiumHelper.IsPremiumAsync();
        BottomBanner.IsVisible = !isPremium;

        CarregarDados();
    }

    private void OnBannerFailedToLoad(object sender, EventArgs e)
    {
        // Opcional: Lógica caso o anúncio falhe ao carregar
    }

    private void CarregarDados()
    {
        string json = Preferences.Default.Get("MinhasViagensJson", string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            _listaViagens = JsonSerializer.Deserialize<List<Viagem>>(json) ?? new();
            _viagemAtual = _listaViagens.FirstOrDefault(v => v.Id == _viagemId);
        }

        if (_viagemAtual == null) return;

        lblNomeViagem.Text = _viagemAtual.Nome;
        lblTotalEstimado.Text = $"Total: R$ {_viagemAtual.ValorTotalEstimado:F2}";
        lblSubtotalPendente.Text = $"Pendente: R$ {_viagemAtual.SubtotalPendente:F2}";

        // Ordena os itens pela propriedade Ordem para manter a estrutura correta
        cvItensViagem.ItemsSource = _viagemAtual.Itens.OrderBy(i => i.Ordem).ToList();
    }

    private void SalvarAlteracoes()
    {
        string json = JsonSerializer.Serialize(_listaViagens);
        Preferences.Default.Set("MinhasViagensJson", json);
    }

    private async void BtnAdicionarTrecho_Clicked(object sender, EventArgs e)
    {
        string tipoAdicao = await DisplayActionSheet("Adicionar à Viagem", "Cancelar", null, "🚗 Novo Trecho (Cidade -> Cidade)", "💸 Outro Gasto (Pedágio, Almoço, etc)");

        if (tipoAdicao == "🚗 Novo Trecho (Cidade -> Cidade)")
        {
            string origem = await DisplayPromptAsync("Origem do Trecho", "De qual cidade/local está saindo?", "OK", "Cancelar");
            if (string.IsNullOrWhiteSpace(origem)) return;

            string destino = await DisplayPromptAsync("Destino do Trecho", "Para qual cidade/local vai?", "OK", "Cancelar");
            if (string.IsNullOrWhiteSpace(destino)) return;

            string distanciaStr = await DisplayPromptAsync("Distância", $"Quantos KM de {origem} até {destino}?", "OK", "Cancelar", keyboard: Keyboard.Numeric);
            if (!double.TryParse(distanciaStr?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double km))
            {
                await DisplayAlert("Erro", "Distância inválida.", "OK");
                return;
            }

            double custoCombustivel = CalcularCustoCombustivel(km);

            var novoItem = new ItemViagem
            {
                Descricao = $"Trecho: {origem} ➔ {destino} ({km} km)",
                Valor = custoCombustivel,
                Ordem = _viagemAtual!.Itens.Count + 1,
                IsConfirmado = false,
                Tipo = "Combustivel"
            };

            _viagemAtual.Itens.Add(novoItem);
            _viagemAtual.ValorTotalEstimado += custoCombustivel;
            _viagemAtual.SubtotalPendente += custoCombustivel;

            SalvarAlteracoes();
            CarregarDados();
        }
        else if (tipoAdicao == "💸 Outro Gasto (Pedágio, Almoço, etc)")
        {
            string descricao = await DisplayPromptAsync("Gasto Extra", "Descrição (Ex: Pedágio Rodovia Bandeirantes):", "OK", "Cancelar");
            if (string.IsNullOrWhiteSpace(descricao)) return;

            string valorStr = await DisplayPromptAsync("Valor", "Valor em R$ (Ex: 11.50):", "OK", "Cancelar", keyboard: Keyboard.Numeric);
            if (!double.TryParse(valorStr?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
            {
                await DisplayAlert("Erro", "Valor inválido.", "OK");
                return;
            }

            var novoItem = new ItemViagem
            {
                Descricao = descricao,
                Valor = valor,
                Ordem = _viagemAtual!.Itens.Count + 1,
                IsConfirmado = false,
                Tipo = "Outros"
            };

            _viagemAtual!.Itens.Add(novoItem);
            _viagemAtual.ValorTotalEstimado += valor;
            _viagemAtual.SubtotalPendente += valor;

            SalvarAlteracoes();
            CarregarDados();
        }
    }

    private double CalcularCustoCombustivel(double km)
    {
        int tipoVeiculo = Preferences.Default.Get("TipoVeiculoIndex", 0);
        if (tipoVeiculo == 0)
        {
            bool usaGasolina = Preferences.Default.Get("UsaGasolina", true);
            string strPreco = usaGasolina ? Preferences.Default.Get("PrecoGasolina", "5.89") : Preferences.Default.Get("PrecoAlcool", "4.29");
            string strRendimento = usaGasolina ? Preferences.Default.Get("KmGasolina", "10") : Preferences.Default.Get("KmAlcool", "8");

            double.TryParse(strPreco.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double preco);
            double.TryParse(strRendimento.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double rendimento);

            if (rendimento > 0)
            {
                return (km / rendimento) * preco;
            }
        }
        else
        {
            return km * 0.15;
        }
        return 0;
    }

    private async void BtnConfirmarGasto_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ItemViagem itemSelecionado) return;
        var item = _viagemAtual?.Itens.FirstOrDefault(i => i.Id == itemSelecionado.Id);
        if (item == null || item.IsConfirmado) return;

        bool confirmar = await DisplayAlert("Confirmar Item", $"Deseja confirmar que '{item.Descricao}' de R$ {item.Valor:F2} foi realizado? Isso enviará o gasto para o seu relatório financeiro.", "Sim", "Não");
        if (!confirmar) return;

        item.IsConfirmado = true;
        _viagemAtual!.SubtotalPendente -= item.Valor;
        if (_viagemAtual.SubtotalPendente < 0) _viagemAtual.SubtotalPendente = 0;

        SalvarAlteracoes();

        var dbService = Handler?.MauiContext?.Services.GetService<Services.DatabaseService>();
        if (dbService != null)
        {
            string categoriaGasto = item.Tipo == "Combustivel" ? "Combustível" : "Outros Gastos";

            await dbService.SalvarRegistroAsync(new Models.RegistroFinanceiro
            {
                Data = DateTime.Now,
                TipoMovimentacao = "Gasto",
                Categoria = categoriaGasto,
                Descricao = $"{_viagemAtual.Nome} - {item.Descricao}",
                Valor = item.Valor
            });
        }

        CarregarDados();
        await DisplayAlert("Sucesso", "Item confirmado e enviado para o Relatório Financeiro!", "OK");
    }

    private async void BtnExcluir_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ItemViagem itemSelecionado) return;
        bool excluir = await DisplayAlert("Excluir", "Deseja remover este item da viagem?", "Sim", "Não");
        if (!excluir) return;

        var item = _viagemAtual?.Itens.FirstOrDefault(i => i.Id == itemSelecionado.Id);
        if (item == null) return;

        if (!item.IsConfirmado)
        {
            _viagemAtual!.SubtotalPendente -= item.Valor;
            if (_viagemAtual.SubtotalPendente < 0) _viagemAtual.SubtotalPendente = 0;
        }
        _viagemAtual!.ValorTotalEstimado -= item.Valor;
        if (_viagemAtual.ValorTotalEstimado < 0) _viagemAtual.ValorTotalEstimado = 0;

        _viagemAtual.Itens.Remove(item);

        SalvarAlteracoes();
        CarregarDados();
    }

    private async void BtnExcluirViagem_Clicked(object sender, EventArgs e)
    {
        bool excluir = await DisplayAlert("Excluir Viagem", $"Deseja apagar permanentemente a viagem '{_viagemAtual?.Nome}' e todos os seus trechos?", "Sim", "Não");
        if (!excluir) return;

        if (_viagemAtual != null)
        {
            _listaViagens.Remove(_viagemAtual);
            SalvarAlteracoes();
        }

        await Navigation.PopAsync();
    }

    private async void BtnEditar_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ItemViagem itemSelecionado) return;
        var item = _viagemAtual?.Itens.FirstOrDefault(i => i.Id == itemSelecionado.Id);
        if (item == null) return;

        string novaDesc = await DisplayPromptAsync("Editar", "Descrição:", initialValue: item.Descricao);
        if (novaDesc == null) return;

        string novoValorStr = await DisplayPromptAsync("Editar", "Novo Valor (R$):", initialValue: item.Valor.ToString(CultureInfo.InvariantCulture), keyboard: Keyboard.Numeric);
        if (!double.TryParse(novoValorStr?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double novoValor)) return;

        double diferenca = novoValor - item.Valor;
        item.Descricao = novaDesc;
        item.Valor = novoValor;

        _viagemAtual!.ValorTotalEstimado += diferenca;
        if (!item.IsConfirmado)
        {
            _viagemAtual.SubtotalPendente += diferenca;
        }

        SalvarAlteracoes();
        CarregarDados();
    }

    private void BtnSubir_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ItemViagem itemSelecionado) return;
        var itensOrdenados = _viagemAtual!.Itens.OrderBy(i => i.Ordem).ToList();
        int index = itensOrdenados.FindIndex(i => i.Id == itemSelecionado.Id);

        if (index > 0)
        {
            var acima = itensOrdenados[index - 1];
            int tempOrdem = itemSelecionado.Ordem;
            itemSelecionado.Ordem = acima.Ordem;
            acima.Ordem = tempOrdem;

            SalvarAlteracoes();
            CarregarDados();
        }
    }

    private void BtnDescer_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ItemViagem itemSelecionado) return;
        var itensOrdenados = _viagemAtual!.Itens.OrderBy(i => i.Ordem).ToList();
        int index = itensOrdenados.FindIndex(i => i.Id == itemSelecionado.Id);

        if (index < itensOrdenados.Count - 1)
        {
            var abaixo = itensOrdenados[index + 1];
            int tempOrdem = itemSelecionado.Ordem;
            itemSelecionado.Ordem = abaixo.Ordem;
            abaixo.Ordem = tempOrdem;

            SalvarAlteracoes();
            CarregarDados();
        }
    }
}