using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Models
{

    public partial class SelectableGroup : ObservableObject
    {
        public Group Group { get; }
        public SelectableGroup(Group g) => Group = g;

        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private bool isVisible = true;

        public string DisplayName => $"{Group.name}".Trim();
    }
}
