using System;
using System.IO;
using System.Collections.Generic;

namespace RenDBCore.Internal
{
	/// <summary>
	/// Serializes or deserializes DiskTreeNodeManager objects.
	/// </summary>
	public class DiskTreeNodeSerializer<K, V> {

		private const int MaxNodeSize = 1024 * 64;

		readonly ITreeNodeManager<K, V> nodeManager;
		readonly ISerializer<K> keySerializer;
		readonly ISerializer<V> valueSerializer;


		public DiskTreeNodeSerializer(ITreeNodeManager<K, V> nodeManager,
			ISerializer<K> keySerializer, ISerializer<V> valueSerializer)
		{
			if(nodeManager == null)
				throw new ArgumentNullException("nodeManager");
			if(keySerializer == null)
				throw new ArgumentNullException("keySerializer");
			if(valueSerializer == null)
				throw new ArgumentNullException("valueSerializer");
			
			this.nodeManager = nodeManager;
			this.keySerializer = keySerializer;
			this.valueSerializer = valueSerializer;
		}

		/// <summary>
		/// Serializes the specified node.
		/// </summary>
		public byte[] Serialize(TreeNode<K, V> node)
		{
			if(valueSerializer.IsFixedSize) {
				if(keySerializer.IsFixedSize)
					return FixedBothSerialize(node);
				return FixedValueSerialize(node);
			}
			
			throw new NotSupportedException("valueSerializer must have a fixed size.");
		}

		/// <summary>
		/// Deserializes specified data.
		/// </summary>
		public TreeNode<K, V> Deserialize(uint id, byte[] data)
		{
			if(valueSerializer.IsFixedSize) {
				if(keySerializer.IsFixedSize)
					return FixedBothDeserialize(id, data);
				return FixedValueDeserialize(id, data);
			}

			throw new NotSupportedException("valueSerializer must have a fixed size.");
		}

		/// <summary>
		/// Serializes specified node, assuming the key and value are fixed length.
		/// </summary>
		byte[] FixedBothSerialize(TreeNode<K, V> node)
		{
			// Precalculate sizes
			int keyLength = keySerializer.Length;
			int valueLength = valueSerializer.Length;
			int entrySize = keyLength + valueLength;
			int bufferSize = 4 + 4 + 4 +
				node.EntryCount * entrySize +
				node.ChildNodesCount * 4;
			
			if(bufferSize >= MaxNodeSize)
				throw new Exception("Serialized node size is too large: " + bufferSize);

			// Create buffer
			byte[] buffer = new byte[bufferSize];
			int bufferOffset = 0;

			// Write parent id of this node.
			BufferHelper.WriteBuffer(node.ParentId, buffer, bufferOffset);
			bufferOffset += 4;

			// Write entry count.
			BufferHelper.WriteBuffer(node.EntryCount, buffer, bufferOffset);
			bufferOffset += 4;

			// Write child id count.
			BufferHelper.WriteBuffer(node.ChildNodesCount, buffer, bufferOffset);
			bufferOffset += 4;

			// Write all entries
			for(int i=0; i<node.EntryCount; i++) {
				var entry = node.GetEntry(i);

				Buffer.BlockCopy(keySerializer.Serialize(entry.Item1), 0, buffer, bufferOffset, keyLength);
				bufferOffset += keyLength;
				Buffer.BlockCopy(valueSerializer.Serialize(entry.Item2), 0, buffer, bufferOffset, valueLength);
				bufferOffset += valueLength;
			}

			// Write all children ids
			var childrenIds = node.ChildrenIdArray;
			for(int i=0; i<childrenIds.Length; i++) {
				BufferHelper.WriteBuffer(childrenIds[i], buffer, bufferOffset);
				bufferOffset += 4;
			}

			// End of serialization
			return buffer;
		}

		/// <summary>
		/// Serializes the specified node, assuming only the value is fixed length.
		/// </summary>
		byte[] FixedValueSerialize(TreeNode<K, V> node)
		{
			using(MemoryStream ms = new MemoryStream()) {
				// Write parent id
				ms.Write(LittleEndian.GetBytes((uint)node.ParentId), 0, 4);

				// Write entry count
				ms.Write(LittleEndian.GetBytes((uint)node.EntryCount), 0, 4);

				// Write children id count
				ms.Write(LittleEndian.GetBytes((uint)node.ChildNodesCount), 0, 4);

				// Write entries
				for(int i=0; i<node.EntryCount; i++) {
					var entry = node.GetEntry(i);
					byte[] key = keySerializer.Serialize(entry.Item1);
					byte[] value = valueSerializer.Serialize(entry.Item2);

					ms.Write(LittleEndian.GetBytes(key.Length), 0, 4);
					ms.Write(key, 0, key.Length);
					ms.Write(value, 0, value.Length);
				}

				// Write children ids
				var childrenIds = node.ChildrenIdArray;
				for(int i=0; i<childrenIds.Length; i++) {
					ms.Write(LittleEndian.GetBytes(childrenIds[i]), 0, 4);
				}

				return ms.ToArray();
			}
		}

		/// <summary>
		/// Deserializes the specified data, assuming the key and value are fixed length.
		/// </summary>
		TreeNode<K, V> FixedBothDeserialize(uint id, byte[] data)
		{
			// Precalculate sizes
			int keyLength = keySerializer.Length;
			int valueLength = valueSerializer.Length;
			int entrySize = keyLength + valueLength;

			int bufferOffset = 0;

			// Read parent id
			uint parentId = BufferHelper.ReadUInt32(data, bufferOffset);
			bufferOffset += 4;

			// Read entry count
			uint entryCount = BufferHelper.ReadUInt32(data, bufferOffset);
			bufferOffset += 4;

			// Read child id count
			uint childIdCount = BufferHelper.ReadUInt32(data, bufferOffset);
			bufferOffset += 4;

			// Read entries
			var entries = new Tuple<K, V>[entryCount];
			for(int i=0; i<entryCount; i++) {
				K key = keySerializer.Deserialize(data, bufferOffset, keyLength);
				bufferOffset += keyLength;
				V value = valueSerializer.Deserialize(data, bufferOffset, valueLength);
				bufferOffset += valueLength;

				entries[i] = new Tuple<K, V>(key, value);
			}

			// Read child ids
			var childrenIds = new uint[childIdCount];
			for(int i=0; i<childIdCount; i++) {
				childrenIds[i] = BufferHelper.ReadUInt32(data, bufferOffset);
				bufferOffset += 4;
			}

			// Create the node
			return new TreeNode<K, V>(nodeManager, id, parentId, entries, childrenIds);
		}

		/// <summary>
		/// Deserializes the specified data, assuming only the value is fixed length.
		/// </summary>
		TreeNode<K, V> FixedValueDeserialize(uint id, byte[] data)
		{
			// Prepare some variables
			int valueLength = valueSerializer.Length;
			int dataOffset = 0;

			// Read parent id
			uint parentId = BufferHelper.ReadUInt32(data, dataOffset);
			dataOffset += 4;

			// Read entry count
			uint entryCount = BufferHelper.ReadUInt32(data, dataOffset);
			dataOffset += 4;

			// Read children id count
			uint childrenIdCount = BufferHelper.ReadUInt32(data, dataOffset);
			dataOffset += 4;

			// Read entries
			var entries = new Tuple<K, V>[entryCount];
			for(int i=0; i<entryCount; i++) {
				int keyLength = BufferHelper.ReadInt32(data, dataOffset);
				dataOffset += 4;
				K key = keySerializer.Deserialize(data, dataOffset, keyLength);
				dataOffset += keyLength;
				V value = valueSerializer.Deserialize(data, dataOffset, valueLength);
				dataOffset += valueLength;

				entries[i] = new Tuple<K, V>(key, value);
			}

			// Read children ids
			var childrenIds = new uint[childrenIdCount];
			for(int i=0; i<childrenIdCount; i++) {
				childrenIds[i] = BufferHelper.ReadUInt32(data, dataOffset);
				dataOffset += 4;
			}

			// Create new node.
			return new TreeNode<K, V>(nodeManager, id, parentId, entries, childrenIds);
		}
	}
}

