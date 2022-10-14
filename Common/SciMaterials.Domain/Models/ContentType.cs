﻿using SciMaterials.Domain.Models.Base;

namespace SciMaterials.Domain.Models;

public class ContentType : NamedModel
{
    public ICollection<File> Files { get; set; } = new HashSet<File>();
}
