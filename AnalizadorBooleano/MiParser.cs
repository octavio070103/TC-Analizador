using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace AnalizadorBooleano
{
    public static class MiParser
    {
        public static Nodo Parsear(string entrada)
        {
            var tokens = Lexer(entrada);
            var parser = new Parser(tokens);
            // Iniciamos desde <CONSULTA>
            var raiz = parser.Consulta();
            // Verificamos que no queden tokens
            if (parser.Actual() != null)
                throw new SyntaxErrorException(
                    $"Token inesperado '{parser.Actual()}' en posición {parser.PosicionActual()}");
            return raiz;
        }

        static List<string> Lexer(string entrada)
        {
            var patron = "\"[^\"]*\"|&&|\\|\\||!|\\(|\\)|\\bAND\\b|\\bOR\\b|\\bNOT\\b|\\w+";
            var matches = Regex.Matches(entrada, patron, RegexOptions.IgnoreCase);
            var lista = new List<string>();
            foreach (Match m in matches)
            {
                var t = m.Value;
                var up = t.ToUpper();
                if (up == "&&" || up == "AND") lista.Add("AND");
                else if (up == "||" || up == "OR") lista.Add("OR");
                else if (up == "!" || up == "NOT") lista.Add("NOT");
                else lista.Add(t);
            }
            return lista;
        }

        class Parser
        {
            private readonly List<string> tokens;
            private int pos;
            public Parser(List<string> tokens) { this.tokens = tokens; pos = 0; }

            public string Actual() => pos < tokens.Count ? tokens[pos] : null;
            public int PosicionActual() => pos < tokens.Count ? pos : tokens.Count;

            void Coincidir(string esperado)
            {
                if (Actual() == esperado) pos++;
                else
                {
                    var e = Actual() ?? "fin de entrada";
                    throw new SyntaxErrorException(
                        $"Se esperaba '{esperado}' pero se encontró '{e}' en posición {PosicionActual()}");
                }
            }

            // 1. <CONSULTA> ::= <EXP>
            public Nodo Consulta()
            {
                var nodoExp = Exp();
                return new Nodo("<CONSULTA>", null, new List<Nodo> { nodoExp });
            }

            // 2–6. <EXP> ::= <EXP> AND <EXP> | <EXP> OR <EXP> | NOT <EXP> | ( <EXP> ) | <TERMINO>
            public Nodo Exp()
            {
                // Empezamos con caso <TERMINO> o NOT o paréntesis
                Nodo nodo;
                if (Actual() == "NOT")
                {
                    // 4. NOT <EXP>
                    Coincidir("NOT");
                    var hijo = Exp();
                    nodo = new Nodo("<EXP>", null, new List<Nodo> {
                        new Nodo("NOT", null),
                        hijo
                    });
                }
                else if (Actual() == "(")
                {
                    // 5. ( <EXP> )
                    Coincidir("(");
                    var sub = Exp();
                    Coincidir(")");
                    nodo = new Nodo("<EXP>", null, new List<Nodo> {
                        new Nodo("(", null),
                        sub,
                        new Nodo(")", null)
                    });
                }
                else
                {
                    // 6. <EXP> ::= <TERMINO>
                    var termino = Termino();
                    nodo = new Nodo("<EXP>", null, new List<Nodo> { termino });
                }

                // 2 y 3: manejamos recursivamente secuencias AND/OR *izquierda asociativa*
                while (Actual() == "AND" || Actual() == "OR")
                {
                    var op = Actual();
                    Coincidir(op);
                    var right = Exp(); // para respetar izquierda asociativa en derivación
                    nodo = new Nodo("<EXP>", null, new List<Nodo> {
                        nodo,
                        new Nodo(op, null),
                        right
                    });
                }

                return nodo;
            }

            // 7–8. <TERMINO> ::= palabra | frase
            public Nodo Termino()
            {
                var token = Actual();
                if (token == null)
                    throw new SyntaxErrorException(
                        $"Se esperaba palabra o frase pero fin de entrada en pos {PosicionActual()}");

                // Terminales
                if (token.StartsWith("\"") && token.EndsWith("\""))
                {
                    Coincidir(token);
                    return new Nodo("<TERMINO>", null, new List<Nodo> {
                        new Nodo("frase", token.Trim('"'))
                    });
                }
                else if (Regex.IsMatch(token, @"^\w+$"))
                {
                    Coincidir(token);
                    return new Nodo("<TERMINO>", null, new List<Nodo> {
                        new Nodo("palabra", token)
                    });
                }
                else
                {
                    throw new SyntaxErrorException(
                        $"Token inválido '{token}' en posición {PosicionActual()}");
                }
            }
        }
    }
}
