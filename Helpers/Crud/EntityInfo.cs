using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace UP01_Smolin_Tournaments.Helpers.Crud
{
    public class PropertyMeta
    {
        public PropertyMeta(PropertyInfo property, EdmProperty edm, bool isKey)
        {
            Property = property;
            Edm = edm;
            IsKey = isKey;
            Type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        }

        public PropertyInfo Property { get; }
        public EdmProperty Edm { get; }
        public bool IsKey { get; }
        public Type Type { get; }
        public PropertyInfo Navigation { get; internal set; }
        public Type RelatedType { get; internal set; }

        public string Name => Property.Name;
        public bool IsNullable => Edm.Nullable;
        public int? MaxLength => Edm.IsMaxLength ? null : Edm.MaxLength;
        public bool IsIdentity => Edm.StoreGeneratedPattern == StoreGeneratedPattern.Identity;
        public bool IsForeignKey => Navigation != null;
        public bool HasTime => Type == typeof(DateTime) && Edm.Precision > 0;
        public bool IsNumber => Type == typeof(int) || Type == typeof(decimal);
        public bool IsMultiLine => Type == typeof(string) && (MaxLength == null || MaxLength > 255);
        public EntityInfo Related => EntityInfo.Of(RelatedType);

        public object GetValue(object entity) => Property.GetValue(entity);
        public void SetValue(object entity, object value) => Property.SetValue(entity, value);
    }

    public class EntityInfo
    {
        private static readonly Dictionary<Type, EntityInfo> Cache = new Dictionary<Type, EntityInfo>();

        public Type Type { get; }
        public string TableName { get; }
        public List<PropertyMeta> Properties { get; }
        public PropertyMeta Key => Properties.Single(p => p.IsKey);

        public static EntityInfo Of(Type type)
        {
            EntityInfo info;
            if (!Cache.TryGetValue(type, out info)) Cache[type] = info = new EntityInfo(type);
            return info;
        }

        private EntityInfo(Type type)
        {
            Type = type;
            MetadataWorkspace workspace = ((IObjectContextAdapter)Db.Ctx).ObjectContext.MetadataWorkspace;
            EntityType entity = workspace.GetItems<EntityType>(DataSpace.CSpace).Single(e => e.Name == type.Name);

            EntitySet table = workspace.GetItems<EntityContainerMapping>(DataSpace.CSSpace).Single()
                .EntitySetMappings.Single(m => m.EntitySet.ElementType.Name == entity.Name)
                .EntityTypeMappings.Single().Fragments.Single().StoreEntitySet;
            TableName = table.Table ?? table.Name;

            Properties = entity.Properties
                .Select(p => new PropertyMeta(type.GetProperty(p.Name), p, entity.KeyProperties.Contains(p)))
                .ToList();

            foreach (NavigationProperty navigation in entity.NavigationProperties)
            {
                EdmProperty column = navigation.GetDependentProperties().SingleOrDefault();
                if (column == null) continue;
                PropertyMeta fk = Properties.Single(p => p.Name == column.Name);
                fk.Navigation = type.GetProperty(navigation.Name);
                fk.RelatedType = fk.Navigation.PropertyType;
            }
        }

        public int KeyOf(object entity) => Convert.ToInt32(Key.GetValue(entity));

        public string DisplayOf(object entity) =>
            entity == null ? "" : Convert.ToString(Type.GetProperty(UiMetadata.DisplayMember(Type)).GetValue(entity));

        public List<object> LoadAll(bool withRelations)
        {
            DbQuery query = Db.Ctx.Set(Type);
            if (withRelations)
                foreach (PropertyMeta p in Properties.Where(p => p.IsForeignKey))
                    query = query.Include(p.Navigation.Name);
            return Enumerable.Cast<object>(query).OrderBy(DisplayOf).ToList();
        }

        public object CopyOf(object entity)
        {
            object copy = Activator.CreateInstance(Type);
            foreach (PropertyMeta p in Properties)
                p.SetValue(copy, p.GetValue(entity));
            return copy;
        }
    }
}
