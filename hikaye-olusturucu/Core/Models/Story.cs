using System;
using System.Collections.Generic;

namespace hikaye_olusturucu.Core.Models;

public class Story
{
    public int Id { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> ImagePaths { get; set; } = new();
    public string AudioPath { get; set; } = string.Empty;
    public string VideoPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}