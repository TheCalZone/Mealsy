#!/usr/bin/env python3
"""Check .NET 8 APK native JNI tokens against the actual bundled Mono.Android.dll.

Uses Python's standard library. Detects stale native metadata that can survive an
incremental publish and cause Mono to abort before the MAUI App constructor.
"""
import argparse
import struct
import zipfile


def unpack(data, offset, fmt):
    return struct.unpack_from('<' + fmt, data, offset)


def text(data, offset):
    return data[offset:data.index(b'\0', offset)].decode('utf-8')


def decompress_assembly(data):
    if data[:4] != b'XALZ':
        return data
    expected = unpack(data, 8, 'I')[0]
    if expected > 64 * 1024 * 1024:
        raise ValueError('Unexpected assembly size')
    source, output, pos = data[12:], bytearray(), 0
    while pos < len(source):
        token = source[pos]
        pos += 1
        literal = token >> 4
        if literal == 15:
            while True:
                extra = source[pos]
                pos += 1
                literal += extra
                if extra != 255:
                    break
        output.extend(source[pos:pos + literal])
        pos += literal
        if pos == len(source):
            break
        distance = unpack(source, pos, 'H')[0]
        pos += 2
        length = (token & 15) + 4
        if (token & 15) == 15:
            while True:
                extra = source[pos]
                pos += 1
                length += extra
                if extra != 255:
                    break
        if not 0 < distance <= len(output) or len(output) + length > expected:
            raise ValueError('Invalid LZ4 assembly data')
        for _ in range(length):
            output.append(output[-distance])
    if len(output) != expected:
        raise ValueError('Truncated assembly')
    return bytes(output)


def managed_tokens(data):
    pe = unpack(data, 0x3c, 'I')[0]
    sections, optional_size = unpack(data, pe + 6, 'H')[0], unpack(data, pe + 20, 'H')[0]
    optional = pe + 24
    directory = optional + (112 if unpack(data, optional, 'H')[0] == 0x20b else 96)
    section_table = optional + optional_size

    def file_offset(rva):
        for i in range(sections):
            size, address, raw_size, raw = unpack(data, section_table + i * 40 + 8, '4I')
            if address <= rva < address + max(size, raw_size):
                return raw + rva - address
        raise ValueError('Unmapped metadata RVA')

    cli = file_offset(unpack(data, directory + 14 * 8, 'I')[0])
    metadata = file_offset(unpack(data, cli + 8, 'I')[0])
    if data[metadata:metadata + 4] != b'BSJB':
        raise ValueError('Invalid CLI metadata')
    cursor = metadata + 16 + unpack(data, metadata + 12, 'I')[0]
    cursor = (cursor + 3) & ~3
    count = unpack(data, cursor + 2, 'H')[0]
    cursor += 4
    streams = {}
    for _ in range(count):
        offset, size = unpack(data, cursor, 'II')
        name = text(data, cursor + 8)
        streams[name] = metadata + offset
        cursor = (cursor + 8 + len(name) + 1 + 3) & ~3
    tables, strings = streams.get('#~', streams.get('#-')), streams['#Strings']
    heaps, valid = data[tables + 6], unpack(data, tables + 8, 'Q')[0]
    rows, cursor = {}, tables + 24
    for table in range(64):
        if valid & (1 << table):
            rows[table] = unpack(data, cursor, 'I')[0]
            cursor += 4
    string_size, guid_size, blob_size = [4 if heaps & bit else 2 for bit in (1, 2, 4)]

    def index_size(table):
        return 4 if rows.get(table, 0) >= 65536 else 2

    def coded_size(bits, *indices):
        return 4 if max(rows.get(i, 0) for i in indices) >= (1 << (16 - bits)) else 2

    sizes = [2 + string_size + guid_size * 3,
             coded_size(2, 0, 26, 35, 1) + string_size * 2,
             4 + string_size * 2 + coded_size(2, 2, 1, 27) + index_size(4) + index_size(6),
             index_size(4), 2 + string_size + blob_size, index_size(6),
             8 + string_size + blob_size + index_size(8)]
    starts = {}
    for table, size in enumerate(sizes):
        starts[table] = cursor
        cursor += rows.get(table, 0) * size

    def integer(offset, size):
        return int.from_bytes(data[offset:offset + size], 'little')

    for row in range(rows.get(2, 0)):
        start = starts[2] + row * sizes[2]
        name = text(data, strings + integer(start + 4, string_size))
        namespace = text(data, strings + integer(start + 4 + string_size, string_size))
        if (namespace, name) != ('Android.Runtime', 'JNIEnvInit'):
            continue
        first = integer(start + sizes[2] - index_size(6), index_size(6))
        last = integer(start + sizes[2] * 2 - index_size(6), index_size(6)) if row + 1 < rows[2] else rows[6] + 1
        result = {'type': 0x02000000 | (row + 1)}
        for method in range(first, last):
            name = text(data, strings + integer(starts[6] + (method - 1) * sizes[6] + 8, string_size))
            if name in ('Initialize', 'RegisterJniNatives'):
                result[name] = 0x06000000 | method
        if len(result) != 3:
            raise ValueError('Missing JNI startup methods')
        return result
    raise ValueError('Missing JNI startup type')


def native_tokens(data):
    if data[:4] != b'\x7fELF' or data[5] != 1:
        raise ValueError('Expected a little-endian ELF library')
    wide = data[4] == 2
    offset = unpack(data, 40 if wide else 32, 'Q' if wide else 'I')[0]
    size, count = unpack(data, 58 if wide else 46, 'HH')
    sections = [unpack(data, offset + i * size, 'II4QII2Q' if wide else '10I') for i in range(count)]
    for section in sections:
        if section[1] != 11:  # SHT_DYNSYM
            continue
        names = sections[section[6]][4]
        for pos in range(section[4], section[4] + section[5], section[9]):
            if wide:
                name, _, _, owner, address, _ = unpack(data, pos, 'IBBHQQ')
            else:
                name, address, _, _, _, owner = unpack(data, pos, 'IIIBBH')
            if text(data, names + name) != 'application_config':
                continue
            owner = sections[owner]
            config = owner[4] + address - owner[3]
            values = unpack(data, config + 40, 'III')
            return dict(zip(('type', 'Initialize', 'RegisterJniNatives'), values))
    raise ValueError('Missing native application_config')


def check(apk):
    with zipfile.ZipFile(apk) as archive:
        line = next(line for line in archive.read('assemblies/assemblies.manifest').decode().splitlines()
                    if line.split()[-1:] == ['Mono.Android'])
        _, _, store, index, _ = line.split()
        libraries = [name for name in archive.namelist() if name.startswith('lib/') and name.endswith('/libxamarin-app.so')]
        if not libraries:
            raise ValueError('No native startup libraries')
        for library in libraries:
            abi = library.split('/')[1]
            blob = 'assemblies/assemblies.blob' if int(store) == 0 else f'assemblies/assemblies.{abi.replace("-", "_")}.blob'
            data = archive.read(blob)
            magic, version, entries, _, _ = unpack(data, 0, '5I')
            if magic != 0x41424158 or version != 1 or int(index) >= entries:
                raise ValueError('Unsupported .NET assembly store')
            offset, length = unpack(data, 20 + int(index) * 24, 'II')
            expected = managed_tokens(decompress_assembly(data[offset:offset + length]))
            actual = native_tokens(archive.read(library))
            if actual != expected:
                raise ValueError(f'{abi}: stale JNI references: native {actual}; bundled assembly {expected}. Clean Release and republish.')
            print(f'{abi}: native JNI startup references match bundled assembly')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('apk')
    args = parser.parse_args()
    try:
        check(args.apk)
    except (ValueError, KeyError, StopIteration, struct.error, IndexError) as error:
        parser.exit(1, f'APK startup check failed: {error}\n')
