using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Models
{
    public partial class SelectableStudent : ObservableObject
    {
        public SelectableStudent(Student s) => Entity = s;

        public Student Entity { get; }

        [ObservableProperty] private bool isSelected;
        [ObservableProperty] private bool isVisible = true;

        public string DisplayName =>
            $"{Entity.surname} {Entity.name} {Entity.middleName}".Trim();
    }
}
