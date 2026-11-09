"""
Раскладывает фото с праздника по папкам с понятными именами.

Берёт скачанную с сервера папку данных (party.db + media/) и копирует фото в:
  <куда>/<игра или раздел>/<время> — <кто снял>.jpg
Аватарки — отдельно, по логину. Материалы ведущего (вопросы Своей игры, звук) не копируются.
Ничего в исходной папке не меняет: база открывается только на чтение.

usage: python tools/export-photos.py <папка с party.db и media> <куда сложить>
"""
import datetime as dt
import os
import re
import shutil
import sqlite3
import sys

src, dst = sys.argv[1], sys.argv[2]

# Разделы без ивента или с понятным названием раздела вместо названия ивента.
SECTIONS = {
    'meme': 'Мемы на ТВ',
    'tvmessage': 'Сообщения на ТВ',
    'auction': 'Лоты аукциона',
}
SKIP_TAGS = {'quiz', 'song'}


def when(raw):
    """Время из базы: EF хранит DateTimeOffset числом (тики/1000 << 11 | смещение в минутах)."""
    if isinstance(raw, str):
        return dt.datetime.fromisoformat(raw).astimezone()
    offset = raw & 0x7FF
    offset = offset - 0x800 if offset & 0x400 else offset
    ticks = (raw >> 11) * 1000
    utc = dt.datetime(1, 1, 1) + dt.timedelta(microseconds=ticks // 10) - dt.timedelta(minutes=offset)
    return utc.replace(tzinfo=dt.timezone.utc).astimezone()


def safe(name):
    return re.sub(r'[<>:"/\\|?*]+', '_', name).strip() or 'без названия'


db = sqlite3.connect(f'file:{os.path.join(src, "party.db")}?mode=ro', uri=True)
logins = dict(db.execute('select Id, Login from Users'))
avatars = {media: user for user, media in db.execute('select Id, AvatarMediaId from Users where AvatarMediaId is not null')}
events = dict(db.execute('select Id, Title from Events'))

copied, missing = 0, 0
for media_id, kind, file_name, author, user, event, tag, created in db.execute(
        'select Id, Kind, FileName, AuthorId, UserId, EventId, Tag, CreatedAt from MediaFiles order by CreatedAt'):
    if kind not in ('Photo', 0) or tag in SKIP_TAGS:
        continue

    source = os.path.join(src, 'media', file_name)
    if not os.path.exists(source):
        missing += 1
        continue

    ext = os.path.splitext(file_name)[1] or '.jpg'
    if media_id in avatars:
        folder, name = 'Аватарки', logins.get(avatars[media_id], 'кто-то')
    else:
        folder = SECTIONS.get(tag) or events.get(event) or 'Разное'
        name = f'{when(created):%Y-%m-%d %H-%M-%S} — {logins.get(author or user, "кто-то")}'

    target_dir = os.path.join(dst, safe(folder))
    os.makedirs(target_dir, exist_ok=True)
    target = os.path.join(target_dir, safe(name) + ext)
    n = 2
    while os.path.exists(target):
        target = os.path.join(target_dir, f'{safe(name)} ({n}){ext}')
        n += 1
    shutil.copy2(source, target)
    copied += 1

print(f'Скопировано фото: {copied}. Файла нет на диске: {missing}. Куда: {os.path.abspath(dst)}')
