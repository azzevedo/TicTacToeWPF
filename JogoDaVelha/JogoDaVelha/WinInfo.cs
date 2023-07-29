using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JogoDaVelha;

/// <summary>
/// Esta classe servirá para desenhar uma linha verde na linha ou coluna vitoriosa
/// </summary>
internal class WinInfo
{
    public WinType Type { get; set; }
    // Representa o número da linha ou coluna
    public int Number { get; set; }
}
