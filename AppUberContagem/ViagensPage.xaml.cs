using AppUberContagem.Models;
using AppUberContagem.Helpers;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace AppUberContagem;

public partial class ViagensPage : ContentPage
{
    private List<Viagem> _listaViagens = new();

    public ViagensPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CarregarViagens();

        bool isPremium = await PremiumHelper.IsPremiumAsync();
        BottomBanner.IsVisible = !isPremium;
    }

    private void CarregarViagens()
    {
        string json = Preferences.Default.Get("MinhasViagensJson", string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                _listaViagens = JsonSerializer.Deserialize<List<Viagem>>(json) ?? new();
            }
            catch
            {
                _listaViagens = new();
            }
        }
        else
        {
            _listaViagens = new();
        }

        cvViagens.ItemsSource = null;
        cvViagens.ItemsSource = _listaViagens;
    }

    private void SalvarTodasViagens()
    {
        string json = JsonSerializer.Serialize(_listaViagens);
        Preferences.Default.Set("MinhasViagensJson", json);
    }

    private async void BtnNovaViagem_Clicked(object sender, EventArgs e)
    {
        // Pede apenas o nome da viagem/rota de forma limpa
        string nomeViagem = await DisplayPromptAsync("Nova Viagem", "Digite o nome da viagem ou destino principal (Ex: Rota Itu x São Paulo):", "OK", "Cancelar");
        if (string.IsNullOrWhiteSpace(nomeViagem)) return;

        // Cria a viagem sem nenhum trecho/gasto automático, deixando tudo zerado
        var novaViagem = new Viagem
        {
            Id = Guid.NewGuid().ToString(), // Garante que o ID único é gerado
            Nome = nomeViagem.Trim(),
            ValorTotalEstimado = 0,
            SubtotalPendente = 0,
            IsFinalizada = false,
            Itens = new List<ItemViagem>() // Lista totalmente vazia pronta para receber itens na tela de detalhes
        };

        _listaViagens.Add(novaViagem);
        SalvarTodasViagens();
        CarregarViagens();

        // Abre direto a tela de detalhes dessa nova viagem para você gerenciar os trechos
        await Navigation.PushAsync(new DetalheViagemPage(novaViagem.Id));
    }

    private async void CardViagem_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not Viagem viagemSelecionada) return;

        // Passamos o ID da viagem para a próxima tela de detalhes
        await Navigation.PushAsync(new DetalheViagemPage(viagemSelecionada.Id));
    }
}