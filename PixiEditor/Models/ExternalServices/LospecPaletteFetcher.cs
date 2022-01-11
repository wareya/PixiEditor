﻿using Newtonsoft.Json;
using PixiEditor.Models.DataHolders;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PixiEditor.Models.ExternalServices
{
    public static class LospecPaletteFetcher
    {
        public const string LospecApiUrl = "https://lospec.com/palette-list";
        public static async Task<PaletteList> FetchPage(int page, string sortingType = "default", string[] tags = null)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = @$"{LospecApiUrl}/load?colorNumberFilterType=any&page={page}&sortingType={sortingType}&tag=";
                    
                    if(tags != null && tags.Length > 0)
                    {
                        url += $"{string.Join(',', tags)}";
                    }

                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        var obj = JsonConvert.DeserializeObject<PaletteList>(content);

                        obj.FetchedCorrectly = obj.Palettes != null;
                        if (obj.Palettes != null)
                        {
                            foreach (var palette in obj.Palettes)
                            {
                                ReadjustColors(palette.Colors);
                            }
                        }

                        return obj;
                    }
                }
            }
            catch(HttpRequestException)
            {
                return new PaletteList() { FetchedCorrectly = false };
            }

            return null;
        }

        private static void ReadjustColors(ObservableCollection<string> colors)
        {
            for (int i = 0; i < colors.Count; i++)
            {
                colors[i] = colors[i].Insert(0, "#");
            }
        }
    }
}
