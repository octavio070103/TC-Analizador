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
            //crea un objeto interno que sabe leer tokens de a uno.
            var parser = new Parser(tokens);
            // Iniciamos desde la regla principal y devuelve el nodo raiz <CONSULTA>
            var raiz = parser.Consulta();
            // Verificamos que no queden tokens(no procesados) arrja un error si pasa algo inesperado
            if (parser.Actual() != null)
                throw new SyntaxErrorException(
                    $"Token inesperado '{parser.Actual()}' en posición {parser.PosicionActual()}");
            return raiz;
        }

        // Lexer: convierte la cadena de entrada en una lista ordenada de tokens
        static List<string> Lexer(string entrada)
        {
            //Usamos una expresión regular para buscar y extraer tokens:
            var patron = "\"[^\"]*\"|&&|\\|\\||!|\\(|\\)|\\bAND\\b|\\bOR\\b|\\bNOT\\b|\\w+";
            var matches = Regex.Matches(entrada, patron, RegexOptions.IgnoreCase);
            var lista = new List<string>();
            // Recorremos los matches y los agregamos a la lista de tokens
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

        // Clase interna Parser:este objeto maneja la gramática y la construcción del árbol sintáctico
        class Parser
        {
            // Lista de tokens del Lexer y posición actual 
            private readonly List<string> tokens;
            private int pos;
            // Constructor: inicializa el parser con la lista de tokens y posición inicial
            public Parser(List<string> tokens) { this.tokens = tokens; pos = 0; }

            // Método Actual: devuelve el token actual o null si se ha llegado al final de la lista
            public string Actual() => pos < tokens.Count ? tokens[pos] : null;
            // Método PosicionActual: devuelve la posición actual en la lista de tokens
            public int PosicionActual() => pos < tokens.Count ? pos : tokens.Count;

            // Método Coincidir: verifica que el token actual coincida con el esperado, avanza la posición si es correcto
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

            // REGLA DE GRAMATICA 1. <CONSULTA> ::= <EXP>
            public Nodo Consulta()
            {
                var nodoExp = Exp();
                return new Nodo("<CONSULTA>", null, new List<Nodo> { nodoExp });
            }

            // REGLA DE GRAMATICA 2–6. <EXP> ::= <EXP> AND <EXP> | <EXP> OR <EXP> | NOT <EXP> | ( <EXP> ) | <TERMINO>
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
                // 5. ( <EXP> )
                else if (Actual() == "(")
                {
                    
                    Coincidir("(");
                    var sub = Exp();
                    Coincidir(")");
                    nodo = new Nodo("<EXP>", null, new List<Nodo> {
                        new Nodo("(", null),
                        sub,
                        new Nodo(")", null)
                    });
                }
                // 6. <EXP> ::= <TERMINO>
                else
                {
                   
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
